using ExamProctoring.Application.Common;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Common.Settings;
using ExamProctoring.Application.Features.AuditLogs.Services;
using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using ExamProctoring.Application.Features.Monitoring.DTOs;
using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    public class ExamAttemptService : IExamAttemptService
    {
        /// Contract status vocabulary for the attempt.
        private const string StatusInProgress = "IN_PROGRESS";
        private const string StatusSubmitted = "SUBMITTED";
        private const string StatusTerminated = "TERMINATED";
        private const string StatusExpired = "EXPIRED";

        /// Exam session states in which a not-yet-started attempt can never begin.
        /// GRACE is deliberately included: it is reserved for continuing an attempt that
        /// already started, never for late entry.
        private static readonly ExamSessionStatus[] NewStartBlockedStates =
        {
            ExamSessionStatus.GRACE,
            ExamSessionStatus.CLOSED,
            ExamSessionStatus.ARCHIVED,
        };

        /// HTTP status frozen into an idempotency record for a successful answer write.
        private const int StatusOk = 200;

        /// Must stay stable: it is how a frozen response body is written and later re-read.
        private static readonly JsonSerializerOptions ResponseJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly IStudentRepository _studentRepository;
        private readonly IAttemptRepository _attemptRepository;
        private readonly IStudentAnswerRepository _studentAnswerRepository;
        private readonly IIdempotencyRepository _idempotencyRepository;
        private readonly IAttemptFinalisationService _finalisationService;
        private readonly IIdentityGate _identityGate;
        private readonly ISystemSettingsRepository _settingsRepository;
        private readonly IAuditLogService _auditLog;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMonitoringNotifier _monitoringNotifier;
        private readonly StudentApplicationSettings _appSettings;
        private readonly MonitoringPolicySettings _monitoringPolicy;
        private readonly ILogger<ExamAttemptService> _logger;

        public ExamAttemptService(
            IStudentRepository studentRepository,
            IAttemptRepository attemptRepository,
            IStudentAnswerRepository studentAnswerRepository,
            IIdempotencyRepository idempotencyRepository,
            IAttemptFinalisationService finalisationService,
            IIdentityGate identityGate,
            ISystemSettingsRepository settingsRepository,
            IAuditLogService auditLog,
            IUnitOfWork unitOfWork,
            IMonitoringNotifier monitoringNotifier,
            IOptions<StudentApplicationSettings> appSettings,
            IOptions<MonitoringPolicySettings> monitoringPolicy,
            ILogger<ExamAttemptService> logger)
        {
            _studentRepository = studentRepository;
            _attemptRepository = attemptRepository;
            _studentAnswerRepository = studentAnswerRepository;
            _idempotencyRepository = idempotencyRepository;
            _finalisationService = finalisationService;
            _identityGate = identityGate;
            _settingsRepository = settingsRepository;
            _auditLog = auditLog;
            _unitOfWork = unitOfWork;
            _monitoringNotifier = monitoringNotifier;
            _appSettings = appSettings.Value;
            _monitoringPolicy = monitoringPolicy.Value;
            _logger = logger;
        }

        public async Task<StartAttemptResult> StartAsync(
            StartAttemptRequest request, int studentId, int examSessionId, string? deviceIdClaim)
        {
            // One server instant drives every timestamp and comparison in this request.
            // request.ClientTimeUtc is never used for timing - only validated and ignored.
            var nowUtc = DateTime.UtcNow;

            // Version gate runs before any state work, so an outdated build can never start an
            // attempt or materialise a paper. Mirrors the ordering in StudentAuthService.
            if (!IsSupportedClientVersion(request.AppVersion))
            {
                _logger.LogInformation(
                    "Start attempt rejected: unsupported client version for student {StudentId}.", studentId);

                return StartAttemptResult.Fail(StartAttemptOutcome.AppVersionUnsupported);
            }

            // The body device and the signed claim must agree; the value used is always the claim.
            if (!DeviceIdentifier.TryResolve(request.DeviceId, deviceIdClaim, out var requestDeviceId))
            {
                // Never log either UUID, only that the comparison failed.
                _logger.LogWarning(
                    "Start attempt rejected for student {StudentId}, session {ExamSessionId}: deviceMatched=false.",
                    studentId, examSessionId);

                return StartAttemptResult.Fail(StartAttemptOutcome.DeviceMismatch);
            }

            // The access token outlives deactivation, so the student is revalidated every request.
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || !student.is_active)
            {
                _logger.LogWarning(
                    "Start attempt rejected: student {StudentId} is missing, deleted or inactive.", studentId);

                return StartAttemptResult.Fail(StartAttemptOutcome.AccountInactive);
            }

            var attempt = await _attemptRepository.GetByExamSessionAsync(studentId, examSessionId);
            if (attempt == null)
            {
                // Missing, unassigned, soft-deleted and DRAFT are deliberately indistinguishable.
                _logger.LogWarning(
                    "Start attempt rejected: session {ExamSessionId} is not a visible assignment for student {StudentId}.",
                    examSessionId, studentId);

                return StartAttemptResult.Fail(StartAttemptOutcome.SessionNotFound);
            }

            var identity = await _identityGate.GetForAttemptAsync(attempt.StudentSessionId);
            if (!identity.IsVerified)
            {
                _logger.LogInformation(
                    "Start attempt rejected: identity not verified for student {StudentId}, session {ExamSessionId}.",
                    studentId, examSessionId);

                return StartAttemptResult.Fail(StartAttemptOutcome.IdentityNotVerified);
            }

            // A finalised attempt is reported, not refused: contract section 3.2 carries
            // SUBMITTED / TERMINATED / EXPIRED in the status field precisely so a client that
            // reopens after submitting learns the attempt is closed and shows its receipt screen.
            // Nothing is created or reset - the timer, paper, device and answers are untouched.
            if (attempt.IsFinalised)
                return StartAttemptResult.Resumed(BuildStartResponse(
                    attempt, attempt.StartedAt ?? nowUtc, attempt.EndsAt ?? nowUtc,
                    attempt.QuestionCount ?? 0, nowUtc, identity,
                    await BuildMonitoringPolicyAsync(attempt),
                    await _studentAnswerRepository.GetForAttemptAsync(attempt.StudentSessionId)));

            if (!IsLifecyclePermitted(attempt, nowUtc, out var blockingStatus))
            {
                _logger.LogInformation(
                    "Start attempt rejected: session {ExamSessionId} lifecycle disallows it (status={Status}, hasStarted={HasStarted}).",
                    examSessionId, blockingStatus, attempt.HasStarted);

                return StartAttemptResult.Fail(StartAttemptOutcome.SessionNotActive, blockingStatus);
            }

            // ----- resume -----
            if (attempt.HasStarted)
                return await ResumeAsync(attempt, requestDeviceId, nowUtc, identity, studentId);

            // ----- first start -----
            var startedAtUtc = nowUtc;

            // A late start inside the login window still receives the full configured duration.
            var endsAtUtc = ExamSessionTiming.PersonalEndsAt(startedAtUtc, attempt.DurationMinutes);

            var claimed = await _attemptRepository.TryClaimFirstStartAsync(
                attempt.StudentSessionId, attempt.PublicId, startedAtUtc, endsAtUtc, requestDeviceId, nowUtc);

            if (!claimed)
            {
                // A concurrent request won the race. Re-read and return that state verbatim, so
                // both callers observe one attempt with one timer and one device binding.
                _logger.LogInformation(
                    "Start attempt for student {StudentId}, session {ExamSessionId} lost the first-start race; resuming.",
                    studentId, examSessionId);

                var winner = await _attemptRepository.GetByExamSessionAsync(studentId, examSessionId);
                if (winner == null)
                    return StartAttemptResult.Fail(StartAttemptOutcome.SessionNotFound);

                return await ResumeAsync(winner, requestDeviceId, nowUtc, identity, studentId);
            }

            var questionCount = await EnsurePaperMaterialisedAsync(attempt, studentId, nowUtc);

            await _auditLog.LogAsync(
                attempt.ExamSessionId, studentId, "Student", "AttemptStarted",
                attempt.StudentSessionId, nameof(StudentSession),
                $"Attempt started; {questionCount} question(s) materialised; endsAtUtc={endsAtUtc:O}");
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Attempt {StudentSessionId} started for student {StudentId}, session {ExamSessionId} with {QuestionCount} question(s).",
                attempt.StudentSessionId, studentId, examSessionId, questionCount);

            await _monitoringNotifier.NotifyStudentStatusChangedAsync(
                attempt.ExamSessionId,
                new StudentStatusChangedDto
                {
                    StudentSessionId = attempt.StudentSessionId,
                    NewStatus = StudentSessionStatus.InExam.ToString(),
                    LoginAtUtc = startedAtUtc,
                });

            // A brand-new attempt cannot have answers yet, but the same builder is used so the
            // two paths cannot drift apart in shape.
            return StartAttemptResult.Started(BuildStartResponse(
                attempt, startedAtUtc, endsAtUtc, questionCount, nowUtc, identity,
                await BuildMonitoringPolicyAsync(attempt),
                await _studentAnswerRepository.GetForAttemptAsync(attempt.StudentSessionId)));
        }

        private async Task<StartAttemptResult> ResumeAsync(
            AttemptView attempt, string requestDeviceId, DateTime nowUtc,
            IdentityGateResult identity, int studentId)
        {
            // The token's device is valid, but this attempt is bound to a different machine.
            // Distinct from DEVICE_MISMATCH, which means the body disagreed with the token.
            if (!DeviceIdentifier.Matches(attempt.DeviceId, requestDeviceId))
            {
                _logger.LogWarning(
                    "Resume rejected for student {StudentId}, attempt {StudentSessionId}: boundDeviceMatched=false.",
                    studentId, attempt.StudentSessionId);

                return StartAttemptResult.Fail(StartAttemptOutcome.AttemptDeviceConflict);
            }

            // Self-heal a paper that never landed because the process died between claiming the
            // start and writing the questions. Never re-shuffles an existing paper.
            var questionCount = await EnsurePaperMaterialisedAsync(attempt, studentId, nowUtc);

            await _monitoringNotifier.NotifyStudentStatusChangedAsync(
                attempt.ExamSessionId,
                new StudentStatusChangedDto
                {
                    StudentSessionId = attempt.StudentSessionId,
                    NewStatus = StudentSessionStatus.InExam.ToString(),
                    LoginAtUtc = attempt.StartedAt,
                });

            return StartAttemptResult.Resumed(BuildStartResponse(
                attempt, attempt.StartedAt!.Value, attempt.EndsAt!.Value, questionCount, nowUtc, identity,
                await BuildMonitoringPolicyAsync(attempt),
                await _studentAnswerRepository.GetForAttemptAsync(attempt.StudentSessionId)));
        }

        /// Returns the authoritative question count, materialising the paper only when the
        /// attempt has none. Resume always reads the already-materialised rows.
        private async Task<int> EnsurePaperMaterialisedAsync(AttemptView attempt, int studentId, DateTime nowUtc)
        {
            var existing = await _attemptRepository.CountMaterialisedQuestionsAsync(attempt.StudentSessionId);
            if (existing > 0)
                return attempt.QuestionCount ?? existing;

            var source = await _attemptRepository.GetBankQuestionsAsync(attempt.QuestionBankId);

            var materialised = QuestionSetMaterialiser.Build(
                source,
                attempt.Randomization,
                attempt.OptionShuffle,
                QuestionSetMaterialiser.ComputeSeed(studentId, attempt.ExamSessionId));

            var entities = materialised.Select(q => new AttemptQuestion
            {
                student_session_id = attempt.StudentSessionId,
                question_id = q.QuestionId,
                public_id = Guid.NewGuid(),
                ordinal = q.Ordinal,
                type = q.Type,
                stem = q.Stem,
                marks = q.Marks,
                created_at = nowUtc,
                Options = q.Options.Select(o => new AttemptQuestionOption
                {
                    public_id = Guid.NewGuid(),
                    ordinal = o.Ordinal,
                    source_slot = o.SourceSlot,
                    label = o.Label,
                    created_at = nowUtc,
                }).ToList(),
            }).ToList();

            var outcome = await _attemptRepository.MaterialiseQuestionSetAsync(
                attempt.StudentSessionId, entities, nowUtc);

            if (outcome == MaterialisationOutcome.AlreadyMaterialised)
            {
                // Another request materialised concurrently; its paper is authoritative.
                _logger.LogInformation(
                    "Paper for attempt {StudentSessionId} was materialised concurrently; using the existing set.",
                    attempt.StudentSessionId);

                return await _attemptRepository.CountMaterialisedQuestionsAsync(attempt.StudentSessionId);
            }

            return entities.Count;
        }

        public async Task<GetAttemptQuestionsResult> GetQuestionsAsync(
            int studentId, int examSessionId, Guid attemptPublicId)
        {
            var nowUtc = DateTime.UtcNow;

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || !student.is_active)
                return GetAttemptQuestionsResult.Fail(GetAttemptQuestionsOutcome.AccountInactive);

            var attempt = await _attemptRepository.GetByPublicIdAsync(studentId, examSessionId, attemptPublicId);

            // A missing attempt, an attempt belonging to another student, and an attempt that
            // has not started are all reported identically so ids cannot be probed.
            if (attempt == null || !attempt.HasStarted)
                return GetAttemptQuestionsResult.Fail(GetAttemptQuestionsOutcome.AttemptNotFound);

            // Locked product decision: the paper is not served after finalisation. It reduces the
            // leak surface and the student needs only the frozen receipt from submit. There is no
            // exam review feature.
            if (attempt.IsFinalised)
                return GetAttemptQuestionsResult.Fail(GetAttemptQuestionsOutcome.AttemptAlreadyFinalised);

            var paper = await _attemptRepository.GetMaterialisedPaperAsync(attempt.StudentSessionId);

            if (paper.Count == 0)
            {
                // Only reachable if a claim succeeded but materialisation never did. Reported
                // honestly as an empty paper; calling Start again repairs it.
                _logger.LogWarning(
                    "Attempt {StudentSessionId} has started but has no materialised paper.",
                    attempt.StudentSessionId);
            }

            var savedAnswers = await LoadSavedAnswersAsync(attempt.StudentSessionId);

            return GetAttemptQuestionsResult.Success(new AttemptQuestionsResponse
            {
                AttemptId = attempt.PublicId,
                ExamSessionId = attempt.ExamSessionId,
                ServerTimeUtc = nowUtc,
                EndsAtUtc = UtcTimestamp.AsUtc(attempt.EndsAt!.Value),
                Questions = paper.Select(q => MapQuestion(q, savedAnswers)).ToList(),
            });
        }

        // =====================================================================================
        // PUT answer
        // =====================================================================================

        /// Operation name recorded on the idempotency record.
        private const string AnswerEndpoint = "PUT_ANSWER";

        public async Task<UpsertAnswerResult> UpsertAnswerAsync(
            UpsertAnswerRequest request,
            int studentId,
            int examSessionId,
            Guid attemptPublicId,
            Guid questionPublicId,
            string? rawIdempotencyKey)
        {
            // One server instant drives savedAt, the deadline comparison and serverTimeUtc.
            var nowUtc = DateTime.UtcNow;

            // Whether the client's key is usable is a business rule, not a binding concern, so it
            // is decided here rather than in the controller.
            if (!IdempotencyKey.TryNormalise(rawIdempotencyKey, out var idempotencyKey))
                return UpsertAnswerResult.Fail(UpsertAnswerOutcome.InvalidIdempotencyKey);

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || !student.is_active)
                return UpsertAnswerResult.Fail(UpsertAnswerOutcome.AccountInactive);

            var attempt = await _attemptRepository.GetByPublicIdAsync(studentId, examSessionId, attemptPublicId);

            // Missing, foreign and never-started attempts are reported identically so ids
            // cannot be probed. An answer never implicitly starts an attempt.
            if (attempt == null || !attempt.HasStarted)
                return UpsertAnswerResult.Fail(UpsertAnswerOutcome.AttemptNotFound);

            if (attempt.IsFinalised)
                return UpsertAnswerResult.Fail(UpsertAnswerOutcome.AttemptAlreadyFinalised);

            // Only an actively-running attempt may be written to.
            if (attempt.Status != StudentSessionStatus.InExam
                && attempt.Status != StudentSessionStatus.Disconnected)
                return UpsertAnswerResult.Fail(UpsertAnswerOutcome.AttemptAlreadyFinalised);

            // Strict server-receipt cutoff. request.ClientAnsweredAtUtc is never consulted here:
            // the device clock is untrusted, so a back-dated timestamp cannot buy extra time.
            if (nowUtc >= attempt.EndsAt!.Value)
                return UpsertAnswerResult.Fail(UpsertAnswerOutcome.AttemptTimeExpired);

            var target = await _attemptRepository.GetAttemptQuestionAsync(attempt.StudentSessionId, questionPublicId);
            if (target == null)
                return UpsertAnswerResult.Fail(UpsertAnswerOutcome.QuestionNotInAttempt);

            // The submitted type must be the type of the question actually being answered.
            if (!QuestionTypeMap.TryFromContract(request.Value!.Type, out var submittedType)
                || submittedType != target.Type)
                return UpsertAnswerResult.Fail(UpsertAnswerOutcome.AnswerTypeMismatch);

            var contentError = ValidateContent(request.Value, target, out var optionIds, out var text);
            if (contentError != null)
                return UpsertAnswerResult.Invalid(contentError);

            // Telemetry only: clamped to the attempt span rather than rejected, so an odd
            // duration never costs a student their answer.
            var durationMs = ClampDuration(request.DurationMs!.Value, attempt);

            UtcTimestamp.TryParse(request.ClientAnsweredAtUtc, out var clientAnsweredAtUtc);

            var response = new UpsertAnswerResponse
            {
                QuestionId = questionPublicId,
                SavedAtUtc = nowUtc,
                ServerTimeUtc = nowUtc,
                EndsAtUtc = UtcTimestamp.AsUtc(attempt.EndsAt.Value),
                DurationMs = durationMs,
                AttemptStatus = MapStatus(attempt, attempt.EndsAt.Value, nowUtc),
            };

            var persist = await _studentAnswerRepository.SaveWithIdempotencyAsync(new AnswerPersistCommand
            {
                StudentSessionId = attempt.StudentSessionId,
                QuestionId = target.QuestionId,
                CanonicalResponse = AnswerValueCodec.Encode(target.Type, optionIds, text),
                DurationMs = durationMs,
                ClientAnsweredAtUtc = clientAnsweredAtUtc == default ? null : clientAnsweredAtUtc,
                NowUtc = nowUtc,
                IdempotencyKey = idempotencyKey,
                Endpoint = AnswerEndpoint,
                ResourceKey = questionPublicId.ToString("D"),
                RequestHash = AnswerRequestCanonicaliser.Hash(
                    questionPublicId, target.Type, optionIds, text, durationMs),
                ResponseStatus = StatusOk,
                ResponseBody = JsonSerializer.Serialize(response, ResponseJsonOptions),
            });

            switch (persist.Outcome)
            {
                case AnswerPersistOutcome.Applied:
                    _logger.LogInformation(
                        "Answer saved for attempt {StudentSessionId}, question {QuestionPublicId}.",
                        attempt.StudentSessionId, questionPublicId);
                    return UpsertAnswerResult.Saved(response);

                case AnswerPersistOutcome.ReplayedExisting:
                    // The original response is returned verbatim, including its original
                    // serverTimeUtc - a replay must not look like a fresh write.
                    var original = JsonSerializer.Deserialize<UpsertAnswerResponse>(
                        persist.ReplayedBody!, ResponseJsonOptions);

                    _logger.LogInformation(
                        "Idempotent replay for attempt {StudentSessionId}, question {QuestionPublicId}.",
                        attempt.StudentSessionId, questionPublicId);

                    return UpsertAnswerResult.Replayed(original ?? response);

                default:
                    _logger.LogWarning(
                        "Idempotency key reused with a different request for attempt {StudentSessionId}.",
                        attempt.StudentSessionId);

                    return UpsertAnswerResult.Fail(UpsertAnswerOutcome.IdempotencyKeyReused);
            }
        }

        // =====================================================================================
        // POST submit
        // =====================================================================================

        public async Task<SubmitAttemptResult> SubmitAsync(
            SubmitAttemptRequest request,
            int studentId,
            int examSessionId,
            Guid attemptPublicId,
            string? rawIdempotencyKey)
        {
            var nowUtc = DateTime.UtcNow;

            if (!IdempotencyKey.TryNormalise(rawIdempotencyKey, out var idempotencyKey))
                return SubmitAttemptResult.Fail(SubmitAttemptOutcome.InvalidIdempotencyKey);

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || !student.is_active)
                return SubmitAttemptResult.Fail(SubmitAttemptOutcome.AccountInactive);

            var attempt = await _attemptRepository.GetByPublicIdAsync(studentId, examSessionId, attemptPublicId);

            // Missing, foreign and never-started attempts are reported identically. A submit
            // never implicitly starts an attempt.
            if (attempt == null || !attempt.HasStarted)
                return SubmitAttemptResult.Fail(SubmitAttemptOutcome.AttemptNotFound);

            // Validation guarantees the reason parses.
            SubmitReasonMap.TryFromContract(request.Reason, out var reason);

            var idempotentRequest = new IdempotencyRequest
            {
                Endpoint = ExamAttemptEndpoints.SubmitAttempt,
                ResourceKey = attemptPublicId.ToString("D"),
                RequestHash = SubmitRequestCanonicaliser.Hash(attemptPublicId, reason),
                ResponseStatus = StatusOk,
                NowUtc = nowUtc,
            };

            // A recorded key short-circuits before any state is touched.
            var existing = await _idempotencyRepository.FindAsync(
                attempt.StudentSessionId, idempotencyKey, idempotentRequest);

            if (existing.Outcome == IdempotencyOutcome.Replay)
                return SubmitAttemptResult.AlreadyFinalised(Deserialize(existing.ReplayedBody!));

            if (existing.Outcome == IdempotencyOutcome.Conflict)
                return SubmitAttemptResult.Fail(SubmitAttemptOutcome.IdempotencyKeyReused);

            // Note there is no deadline check here. Submit is a closing action: an attempt whose
            // timer has run out must still be finalisable, which is exactly what
            // CLIENT_TIMER_EXPIRED is for. Only answer writes are refused after ends_at.
            var finalisation = await _finalisationService.FinaliseAsync(new AttemptFinalisationContext
            {
                StudentSessionId = attempt.StudentSessionId,
                ExamSessionId = attempt.ExamSessionId,
                CourseTag = attempt.CourseTag,
                QuestionCount = attempt.QuestionCount ?? 0,
                Reason = reason,
                IsAlreadyTerminated = attempt.Status == StudentSessionStatus.Terminated,
                ActorId = studentId,
                ActorType = "Student",
            });

            // A receipt without scores is worse than a retry: the student has left the exam and
            // this response is the only thing their receipt screen can render. Failing here
            // surfaces as 500 SERVER_ERROR, and because the attempt is already finalised the
            // retry re-enters the already-finalised path and grades it then.
            if (finalisation.Grading == null)
                throw new InvalidOperationException(
                    $"Attempt {attempt.StudentSessionId} finalised but could not be graded; " +
                    "refusing to return a receipt without a grading snapshot.");

            var response = BuildSubmitResponse(finalisation.Snapshot, finalisation.Grading, nowUtc);

            // Recorded only after finalisation is durable, so a crash can lose the record but
            // never claim a success that did not happen. A concurrent insert of the same key is
            // resolved against the winner's record.
            idempotentRequest.ResponseBody = JsonSerializer.Serialize(response, ResponseJsonOptions);

            var stored = await _idempotencyRepository.StoreAsync(
                attempt.StudentSessionId, idempotencyKey, idempotentRequest);

            if (stored.Outcome == IdempotencyOutcome.Conflict)
                return SubmitAttemptResult.Fail(SubmitAttemptOutcome.IdempotencyKeyReused);

            if (stored.Outcome == IdempotencyOutcome.Replay)
                return SubmitAttemptResult.AlreadyFinalised(Deserialize(stored.ReplayedBody!));

            _logger.LogInformation(
                "Attempt {StudentSessionId} submit handled: outcome={Outcome} reason={Reason} receipt present={HasReceipt}.",
                attempt.StudentSessionId, finalisation.Status, reason,
                !string.IsNullOrEmpty(response.ReceiptCode));

            if (finalisation.Status == AttemptFinalisationStatus.Finalised)
            {
                await _monitoringNotifier.NotifyStudentStatusChangedAsync(
                    attempt.ExamSessionId,
                    new StudentStatusChangedDto
                    {
                        StudentSessionId = attempt.StudentSessionId,
                        NewStatus = finalisation.Snapshot.Status.ToString(),
                        LoginAtUtc = attempt.StartedAt,
                    });
            }

            return finalisation.Status == AttemptFinalisationStatus.Finalised
                ? SubmitAttemptResult.Finalised(response)
                : SubmitAttemptResult.AlreadyFinalised(response);
        }

        private static SubmitAttemptResponse Deserialize(string body) =>
            JsonSerializer.Deserialize<SubmitAttemptResponse>(body, ResponseJsonOptions)
            ?? new SubmitAttemptResponse();

        /// Every field except serverTimeUtc comes from frozen persisted state, which is what makes
        /// the receipt identical on the first call and on every retry afterwards.
        private static SubmitAttemptResponse BuildSubmitResponse(
            AttemptFinalSnapshot snapshot, GradingSnapshotDto grading, DateTime nowUtc) =>
            new()
            {
                AttemptId = snapshot.AttemptPublicId,
                Status = MapFinalStatus(snapshot),
                FinalisedAtUtc = UtcTimestamp.AsUtc(snapshot.FinalisedAtUtc),
                ServerTimeUtc = nowUtc,
                AnsweredCount = snapshot.AnsweredCount,
                QuestionCount = snapshot.QuestionCount,
                ReceiptCode = snapshot.ReceiptCode,
                Grading = grading,
            };

        /// The contract's terminal vocabulary. EXPIRED is reserved for the case where nobody
        /// submitted at all and the server closed the attempt itself; a client that submits at
        /// timer zero has still submitted.
        private static string MapFinalStatus(AttemptFinalSnapshot snapshot)
        {
            if (snapshot.Status == StudentSessionStatus.Terminated
                || snapshot.Reason == AttemptFinalisationReason.ProctorTerminated)
                return StatusTerminated;

            return snapshot.Reason == AttemptFinalisationReason.ServerExpiry
                ? StatusExpired
                : StatusSubmitted;
        }

        /// Per-type content rules, checked against the student's own materialised paper rather
        /// than against the authored Question columns. Returns null when acceptable.
        private static string? ValidateContent(
            AnswerValueDto value, AttemptQuestionTargetView target,
            out List<Guid> optionIds, out string? text)
        {
            optionIds = new List<Guid>();
            text = null;

            if (QuestionTypeMap.UsesOptions(target.Type))
            {
                if (!string.IsNullOrEmpty(value.Text))
                    return "Text is not allowed for an option-based question.";

                optionIds = value.OptionIds ?? new List<Guid>();

                // Strict: the contract never says duplicates are collapsed, so a repeated id is
                // treated as a malformed selection rather than silently de-duplicated.
                if (optionIds.Count != optionIds.Distinct().Count())
                    return "Duplicate option ids are not allowed.";

                // Every id must belong to THIS question of THIS attempt.
                var permitted = target.OptionPublicIds.ToHashSet();
                if (optionIds.Any(id => !permitted.Contains(id)))
                    return "One or more option ids do not belong to this question.";

                // MCQ_MULTI accepts zero or more; an empty array is the documented clear.
                if (target.Type != QuestionType.MultipleChoiceMulti && optionIds.Count != 1)
                    return "Exactly one option id is required for this question type.";

                return null;
            }

            if (value.OptionIds is { Count: > 0 })
                return "Option ids are not allowed for a text question.";

            // Trimmed then measured, so trailing whitespace never pushes a valid answer over
            // the limit. Text is never silently truncated - an over-long answer is refused.
            text = (value.Text ?? string.Empty).Trim();

            var maxLength = QuestionTypeMap.TextMaxLength(target.Type);
            if (maxLength.HasValue && text.Length > maxLength.Value)
                return $"Text must not exceed {maxLength.Value} characters.";

            return null;
        }

        /// Bounded by the attempt's own span: a student cannot have spent longer on one question
        /// than the whole exam lasts.
        private static int ClampDuration(int durationMs, AttemptView attempt)
        {
            if (durationMs < 0)
                return 0;

            var spanMs = (attempt.EndsAt!.Value - attempt.StartedAt!.Value).TotalMilliseconds;
            var upperBound = spanMs > int.MaxValue ? int.MaxValue : (int)spanMs;

            return durationMs > upperBound ? upperBound : durationMs;
        }

        private static AttemptQuestionDto MapQuestion(
            AttemptQuestionView question, IReadOnlyDictionary<Guid, PersistedAnswerView> savedAnswers)
        {
            var maxLength = QuestionTypeMap.TextMaxLength(question.Type);

            return new AttemptQuestionDto
            {
                QuestionId = question.PublicId,
                Ordinal = question.Ordinal,
                Type = QuestionTypeMap.ToContract(question.Type),
                Stem = question.Stem,
                Marks = question.Marks,
                Options = question.Options
                    .OrderBy(o => o.Ordinal)
                    .Select(o => new AttemptQuestionOptionDto
                    {
                        OptionId = o.PublicId,
                        Label = o.Label,
                    })
                    .ToList(),

                // Null when the student has never saved an answer. A deliberately cleared
                // answer keeps its row, so it comes back non-null with empty content - which is
                // what keeps "cleared" distinguishable from "never answered".
                SavedAnswer = savedAnswers.TryGetValue(question.PublicId, out var saved)
                    ? ToSavedAnswerContent(saved)
                    : null,

                Constraints = maxLength.HasValue
                    ? new AttemptQuestionConstraintsDto { MaxLength = maxLength.Value }
                    : null,
            };
        }

        /// Timing is decided arithmetically from start_time rather than from the persisted
        /// ExamSession.status, which the background transition service only refreshes every
        /// few minutes. The status is still consulted for the definitively-over states.
        private static bool IsLifecyclePermitted(AttemptView attempt, DateTime nowUtc, out string blockingStatus)
        {
            blockingStatus = attempt.ExamSessionStatus.ToString();

            // Over for every purpose, including continuing an attempt that had started.
            if (attempt.ExamSessionStatus is ExamSessionStatus.CLOSED or ExamSessionStatus.ARCHIVED)
                return false;

            // A started attempt may continue, including through GRACE.
            if (attempt.HasStarted)
                return true;

            if (NewStartBlockedStates.Contains(attempt.ExamSessionStatus))
                return false;

            return ExamSessionTiming.IsLoginWindowOpen(nowUtc, attempt.StartTime, attempt.LoginWindowMinutes);
        }

        private bool IsSupportedClientVersion(string? submittedVersion)
        {
            // Startup validation guarantees the configured minimum parses.
            AppVersion.TryParse(_appSettings.MinimumSupportedVersion, out var minimum);

            return AppVersion.TryParse(submittedVersion, out var submitted) && submitted >= minimum;
        }

        /// Two of the five values have a database home today; the rest come from configuration
        /// until the realtime monitoring feature defines where they belong.
        private async Task<MonitoringPolicyDto> BuildMonitoringPolicyAsync(AttemptView attempt)
        {
            // Null-safe on purpose: SystemSettings is only seeded by the development demo data,
            // so the row is legitimately absent in other environments.
            var settings = await _settingsRepository.GetAsync();

            return new MonitoringPolicyDto
            {
                GazeDeviationThresholdSeconds = attempt.EyeGazeThresholdSec,
                AudioMonitoringEnabled = settings?.ambient_audio_monitoring
                                         ?? _monitoringPolicy.AudioMonitoringEnabledFallback,
                AudioNoiseThresholdDb = _monitoringPolicy.AudioNoiseThresholdDb,
                HeartbeatIntervalSeconds = _monitoringPolicy.HeartbeatIntervalSeconds,
                ConnectivityLostThresholdSeconds = _monitoringPolicy.ConnectivityLostThresholdSeconds,
            };
        }

        /// Saved answers keyed by the per-attempt question public id, ready to merge into either
        /// student read path.
        private async Task<Dictionary<Guid, PersistedAnswerView>> LoadSavedAnswersAsync(int studentSessionId)
        {
            var answers = await _studentAnswerRepository.GetForAttemptAsync(studentSessionId);

            return answers.ToDictionary(a => a.AttemptQuestionPublicId);
        }

        private static SavedAnswerContentDto ToSavedAnswerContent(PersistedAnswerView saved) =>
            new()
            {
                Value = AnswerValueCodec.Decode(saved.StoredResponse) ?? new AnswerValueDto(),
                DurationMs = saved.DurationMs,
                SavedAtUtc = UtcTimestamp.AsUtc(saved.SavedAtUtc),
            };

        private static StartAttemptResponse BuildStartResponse(
            AttemptView attempt, DateTime startedAtUtc, DateTime endsAtUtc, int questionCount,
            DateTime nowUtc, IdentityGateResult identity, MonitoringPolicyDto monitoringPolicy,
            IReadOnlyCollection<PersistedAnswerView> savedAnswers) =>
            new()
            {
                AttemptId = attempt.PublicId,
                ExamSessionId = attempt.ExamSessionId,
                StudentSessionId = attempt.StudentSessionId,
                Status = MapStatus(attempt, endsAtUtc, nowUtc),
                StartedAtUtc = UtcTimestamp.AsUtc(startedAtUtc),
                EndsAtUtc = UtcTimestamp.AsUtc(endsAtUtc),
                ServerTimeUtc = nowUtc,
                GraceEndsAtUtc = attempt.GracePeriodEndedAt.HasValue
                    ? UtcTimestamp.AsUtc(attempt.GracePeriodEndedAt.Value)
                    : null,
                Identity = new AttemptIdentityDto
                {
                    Method = identity.Method ?? string.Empty,
                    VerifiedAtUtc = UtcTimestamp.AsUtc(identity.VerifiedAtUtc ?? startedAtUtc),
                },
                MonitoringPolicy = monitoringPolicy,

                // Empty on a first start; on resume this is how the client restores the answers
                // it had already sent before the crash.
                SavedAnswers = savedAnswers
                    .Select(a => new SavedAnswerDto
                    {
                        QuestionId = a.AttemptQuestionPublicId,
                        Value = AnswerValueCodec.Decode(a.StoredResponse) ?? new AnswerValueDto(),
                        DurationMs = a.DurationMs,
                        SavedAtUtc = UtcTimestamp.AsUtc(a.SavedAtUtc),
                    })
                    .ToList(),

                QuestionCount = questionCount,
            };

        private static string MapStatus(AttemptView attempt, DateTime endsAtUtc, DateTime nowUtc) =>
            attempt.Status switch
            {
                StudentSessionStatus.Submitted => StatusSubmitted,
                StudentSessionStatus.Terminated => StatusTerminated,

                // The personal deadline has passed but nothing has finalised the attempt yet;
                // Phase 3 owns the finalisation itself.
                _ when nowUtc >= endsAtUtc => StatusExpired,

                // Disconnected is still an open attempt from the client's point of view.
                _ => StatusInProgress,
            };
    }
}
