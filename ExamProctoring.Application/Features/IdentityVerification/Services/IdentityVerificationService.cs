using ExamProctoring.Application.Common;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Eligibility.DTOs;
using ExamProctoring.Application.Features.Eligibility.Services;
using ExamProctoring.Application.Features.IdentityVerification.DTOs;
using ExamProctoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.IdentityVerification.Services
{
    /// Identity verification for the student desktop client (FR-01).
    ///
    /// The backend runs no face inference. Both vectors already exist by the time they reach
    /// this class: the reference was produced by the trusted administrative import, the probe
    /// by the client's own SFace pipeline. This service decides admission, budget, idempotency,
    /// liveness plausibility and - using the configured threshold - the verdict.
    ///
    /// Two rules shape almost every ordering decision below:
    ///
    /// 1. Only a completed comparison may consume an attempt. A malformed request, a model
    ///    mismatch, an unconfigured threshold or a dropped connection must all cost nothing,
    ///    because a student who loses an exam to a network problem has been failed by us.
    /// 2. Nothing here ever logs, echoes or persists an embedding. The probe is compared and
    ///    discarded; the reference never leaves this method.
    public class IdentityVerificationService : IIdentityVerificationService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IStudentSessionRepository _studentSessionRepository;
        private readonly IIdentityVerificationRepository _identityVerificationRepository;
        private readonly IEligibilityService _eligibilityService;
        private readonly ISystemSettingsRepository _systemSettingsRepository;
        private readonly IFaceMatcher _faceMatcher;
        private readonly ILogger<IdentityVerificationService> _logger;

        /// Used only if SystemSettings has somehow never been seeded. The contract's default.
        private const int DefaultMaxAttempts = 3;

        public IdentityVerificationService(
            IStudentRepository studentRepository,
            IStudentSessionRepository studentSessionRepository,
            IIdentityVerificationRepository identityVerificationRepository,
            IEligibilityService eligibilityService,
            ISystemSettingsRepository systemSettingsRepository,
            IFaceMatcher faceMatcher,
            ILogger<IdentityVerificationService> logger)
        {
            _studentRepository = studentRepository;
            _studentSessionRepository = studentSessionRepository;
            _identityVerificationRepository = identityVerificationRepository;
            _eligibilityService = eligibilityService;
            _systemSettingsRepository = systemSettingsRepository;
            _faceMatcher = faceMatcher;
            _logger = logger;
        }

        // ---------------------------------------------------------------------------------
        // POST /api/v1/identity/verification-sessions
        // ---------------------------------------------------------------------------------

        public async Task<CreateVerificationSessionResult> CreateOrResumeAsync(
            int studentId, string? deviceIdClaim)
        {
            var nowUtc = DateTime.UtcNow;

            // The access token outlives deactivation, so the student is revalidated here.
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || !student.is_active)
            {
                _logger.LogWarning(
                    "Verification session refused: student {StudentId} is missing, deleted or inactive.",
                    studentId);

                return CreateVerificationSessionResult.Fail(
                    CreateVerificationSessionOutcome.AccountInactive);
            }

            // Admission reuses the eligibility decision wholesale rather than re-implementing
            // session selection. Duplicating that algorithm is how two endpoints end up
            // disagreeing about which exam a student is sitting.
            var eligibility = await _eligibilityService.GetEligibilityAsync(studentId);

            if (eligibility.Status != EligibilityStatus.Resolved || eligibility.Response?.Session == null)
            {
                _logger.LogInformation(
                    "Verification session refused for student {StudentId}: no admitted exam session (status={Status}).",
                    studentId, eligibility.Status);

                return CreateVerificationSessionResult.Fail(
                    CreateVerificationSessionOutcome.SessionNotAdmitted);
            }

            var examSessionId = eligibility.Response.Session.Id;

            var assignment = await _studentSessionRepository
                .GetVisibleAssignmentAsync(studentId, examSessionId);

            if (assignment == null)
                return CreateVerificationSessionResult.Fail(
                    CreateVerificationSessionOutcome.SessionNotAdmitted);

            var existing = await _identityVerificationRepository
                .GetByStudentSessionAsync(assignment.StudentSessionId);

            // Checked before eligibility's own verdict: a student who has already verified and
            // started their exam is no longer "eligible" to start one, and telling them they
            // are not admitted would send them to the proctor instead of into the exam.
            if (existing is { IsVerified: true })
                return CreateVerificationSessionResult.Fail(
                    CreateVerificationSessionOutcome.AlreadyVerified);

            if (!eligibility.Response.IsEligible)
            {
                _logger.LogInformation(
                    "Verification session refused for student {StudentId}, session {ExamSessionId}: reason={ReasonCode}.",
                    studentId, examSessionId, eligibility.Response.ReasonCode ?? "(none)");

                return CreateVerificationSessionResult.Fail(
                    CreateVerificationSessionOutcome.SessionNotAdmitted);
            }

            if (existing != null)
            {
                // Resume. The attempt count is returned exactly as stored - a client that
                // crashed after two failures must not reopen to a fresh three.
                _logger.LogInformation(
                    "Verification session resumed for student {StudentId}, session {ExamSessionId}: {Used}/{Max} attempts used.",
                    studentId, examSessionId, existing.AttemptsUsed, existing.MaxAttempts);

                return CreateVerificationSessionResult.Resumed(
                    BuildSessionResponse(existing, examSessionId));
            }

            var maxAttempts = await ResolveMaxAttemptsAsync();

            // CreateAsync is race-safe: a parallel open of the client returns the row the
            // other request created rather than minting a second budget. The loser of that
            // race reports 200, so only one request in a race ever claims a 201.
            var (created, wasCreated) = await _identityVerificationRepository.CreateAsync(
                assignment.StudentSessionId, maxAttempts, deviceIdClaim, nowUtc);

            _logger.LogInformation(
                "Verification session {Action} for student {StudentId}, session {ExamSessionId} with {Max} attempt(s).",
                wasCreated ? "created" : "resumed after a concurrent create",
                studentId, examSessionId, created.MaxAttempts);

            var response = BuildSessionResponse(created, examSessionId);

            return wasCreated
                ? CreateVerificationSessionResult.Created(response)
                : CreateVerificationSessionResult.Resumed(response);
        }

        // ---------------------------------------------------------------------------------
        // POST /api/v1/identity/verification-sessions/{id}/attempts
        // ---------------------------------------------------------------------------------

        public async Task<SubmitVerificationAttemptResult> SubmitAttemptAsync(
            SubmitVerificationAttemptRequest request, int studentId, Guid verificationSessionId)
        {
            var nowUtc = DateTime.UtcNow;

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || !student.is_active)
                return SubmitVerificationAttemptResult.Fail(
                    SubmitVerificationAttemptOutcome.AccountInactive);

            // ----- checks that must never consume an attempt -----
            // All of these run before any budget is touched, in order of cheapness.

            // This backend's pinned pair. Checked before the reference is even loaded so an
            // outdated client gets the same answer whether or not it has been enrolled.
            if (!FaceEmbedding.IsSupportedModel(request.EmbeddingModel)
                || !FaceEmbedding.IsSupportedVersion(request.EmbeddingVersion))
            {
                _logger.LogWarning(
                    "Verification attempt refused for student {StudentId}: unsupported embedding model/version submitted.",
                    studentId);

                return SubmitVerificationAttemptResult.Fail(
                    SubmitVerificationAttemptOutcome.ModelMismatch);
            }

            // Length, finiteness and norm. The vector itself is never logged - only that it
            // failed, and how.
            if (!FaceEmbedding.TryCanonicalise(request.Embedding, out var probe, out var embeddingError))
            {
                _logger.LogWarning(
                    "Verification attempt refused for student {StudentId}: embedding rejected ({Reason}).",
                    studentId, embeddingError);

                return SubmitVerificationAttemptResult.Fail(
                    SubmitVerificationAttemptOutcome.EmbeddingInvalid);
            }

            // Ownership is enforced inside the query: a foreign or unknown id is one answer.
            var session = await _identityVerificationRepository
                .GetByPublicIdAsync(studentId, verificationSessionId);

            if (session == null)
                return SubmitVerificationAttemptResult.Fail(
                    SubmitVerificationAttemptOutcome.SessionNotFound);

            if (session.IsVerified)
                return SubmitVerificationAttemptResult.Fail(
                    SubmitVerificationAttemptOutcome.AlreadyVerified);

            // Fast-path replay. The authoritative check is the unique index inside
            // RecordAttemptAsync; this only saves the work when the retry is not a race.
            var replay = await _identityVerificationRepository
                .FindAttemptAsync(session.Id, request.ClientAttemptId!);

            if (replay != null)
            {
                _logger.LogInformation(
                    "Verification attempt replayed for student {StudentId}: clientAttemptId already recorded.",
                    studentId);

                return SubmitVerificationAttemptResult.Replayed(BuildAttemptResponse(replay));
            }

            var reference = await _identityVerificationRepository.GetReferenceFaceAsync(studentId);

            // No trusted reference: a business outcome, not an error, and deliberately not
            // retryable. It consumes no attempt because nothing was compared - the student
            // cannot influence this and must not be charged for it.
            //
            // The photo is NOT downloaded and no embedding is generated here. Reference
            // vectors come only from the administrative import.
            if (reference is not { IsEnrolled: true })
            {
                _logger.LogWarning(
                    "Verification attempt for student {StudentId} found no trusted reference embedding.",
                    studentId);

                return await RecordAsync(session, request, IdentityVerificationOutcome.NO_ENROLLED_FACE,
                    consumesAttempt: false, matchScore: null, thresholdUsed: null,
                    livenessAccepted: false, livenessRejectionReason: null, nowUtc);
            }

            // The enrolled reference must have been produced by the same model as the probe.
            // Comparing across models yields a meaningless score, and the failure would look
            // like mass impersonation rather than a configuration error.
            if (!FaceEmbedding.IsSupportedModel(reference.Model)
                || !FaceEmbedding.IsSupportedVersion(reference.ModelVersion))
            {
                _logger.LogError(
                    "Verification attempt for student {StudentId} blocked: enrolled reference model/version does not match the pinned SFace pair.",
                    studentId);

                return SubmitVerificationAttemptResult.Fail(
                    SubmitVerificationAttemptOutcome.ModelMismatch);
            }

            // A stored blob of the wrong size is a corrupt row, not a mismatch. Treated as an
            // enrolment problem so the student is sent to a proctor rather than shown a score.
            if (!FaceEmbedding.TryFromStorage(reference.Embedding, out var referenceVector))
            {
                _logger.LogError(
                    "Verification attempt for student {StudentId} blocked: stored reference embedding is malformed.",
                    studentId);

                return await RecordAsync(session, request, IdentityVerificationOutcome.NO_ENROLLED_FACE,
                    consumesAttempt: false, matchScore: null, thresholdUsed: null,
                    livenessAccepted: false, livenessRejectionReason: null, nowUtc);
            }

            // Fail closed. A missing threshold is a server configuration fault, and comparing
            // against an invented number would either lock legitimate students out or admit
            // impersonators - both silently. No attempt is consumed.
            var threshold = await ResolveThresholdAsync();
            if (threshold == null)
            {
                _logger.LogError(
                    "Verification attempt for student {StudentId} blocked: SystemSettings.sface_cosine_threshold is not configured. " +
                    "Identity verification cannot run until a calibrated SFace cosine threshold is set.",
                    studentId);

                return SubmitVerificationAttemptResult.Fail(
                    SubmitVerificationAttemptOutcome.ThresholdNotConfigured);
            }

            // ----- from here on the attempt is a completed comparison and consumes budget -----

            var liveness = request.Liveness!;

            var livenessAccepted = LivenessPolicy.TryValidate(
                liveness.BlinkCount!.Value,
                liveness.FramesAnalysed!.Value,
                liveness.DurationMs!.Value,
                liveness.MinEyeOpenness!.Value,
                liveness.MaxEyeOpenness!.Value,
                out var livenessRejectionReason);

            if (!livenessAccepted)
            {
                // A proctoring incident, not a UX problem: the client refuses to submit without
                // a real blink, so impossible evidence means the client was modified.
                _logger.LogWarning(
                    "Liveness evidence rejected for student {StudentId}, verification session {VerificationSessionId}: {Reason}.",
                    studentId, session.Id, livenessRejectionReason);

                return await RecordAsync(session, request, IdentityVerificationOutcome.LIVENESS_REJECTED,
                    consumesAttempt: true, matchScore: null, thresholdUsed: threshold,
                    livenessAccepted: false, livenessRejectionReason, nowUtc);
            }

            // The only place a verdict is reached. The client never makes this decision.
            var score = _faceMatcher.Similarity(referenceVector, probe);

            var outcome = score >= threshold.Value
                ? IdentityVerificationOutcome.MATCHED
                : IdentityVerificationOutcome.NOT_MATCHED;

            _logger.LogInformation(
                "Verification attempt completed for student {StudentId}, verification session {VerificationSessionId}: outcome={Outcome}.",
                studentId, session.Id, outcome);

            return await RecordAsync(session, request, outcome,
                consumesAttempt: true, matchScore: score, thresholdUsed: threshold,
                livenessAccepted: true, livenessRejectionReason: null, nowUtc);
        }

        // ---------------------------------------------------------------------------------

        /// Hands the decided outcome to the repository, which applies it atomically. The
        /// budget guard and the MATCHED transition both live in that one transaction, so a
        /// concurrent duplicate cannot double-consume and a race cannot verify twice.
        private async Task<SubmitVerificationAttemptResult> RecordAsync(
            VerificationSessionView session,
            SubmitVerificationAttemptRequest request,
            IdentityVerificationOutcome outcome,
            bool consumesAttempt,
            double? matchScore,
            double? thresholdUsed,
            bool livenessAccepted,
            string? livenessRejectionReason,
            DateTime nowUtc)
        {
            var liveness = request.Liveness!;

            UtcTimestamp.TryParse(request.CapturedAtUtc, out var capturedAtUtc);

            var result = await _identityVerificationRepository.RecordAttemptAsync(new RecordAttemptCommand
            {
                VerificationSessionId = session.Id,
                StudentSessionId = session.StudentSessionId,
                ClientAttemptId = request.ClientAttemptId!,

                Outcome = outcome,
                ConsumesAttempt = consumesAttempt,
                MatchScore = matchScore,
                ThresholdUsed = thresholdUsed,

                LivenessAccepted = livenessAccepted,
                LivenessBlinkCount = liveness.BlinkCount ?? 0,
                LivenessFramesAnalysed = liveness.FramesAnalysed ?? 0,
                LivenessDurationMs = liveness.DurationMs ?? 0,
                LivenessMinEyeOpenness = liveness.MinEyeOpenness ?? 0,
                LivenessMaxEyeOpenness = liveness.MaxEyeOpenness ?? 0,
                LivenessRejectionReason = livenessRejectionReason,

                EmbeddingModel = FaceEmbedding.Model,
                EmbeddingModelVersion = FaceEmbedding.ModelVersion,

                CapturedAtUtc = capturedAtUtc == default ? null : capturedAtUtc,
                NowUtc = nowUtc,
            });

            return result.Status switch
            {
                RecordAttemptStatus.Recorded =>
                    SubmitVerificationAttemptResult.Completed(BuildAttemptResponse(result.Attempt!)),

                RecordAttemptStatus.Replayed =>
                    SubmitVerificationAttemptResult.Replayed(BuildAttemptResponse(result.Attempt!)),

                RecordAttemptStatus.NoAttemptsRemaining =>
                    SubmitVerificationAttemptResult.Fail(
                        SubmitVerificationAttemptOutcome.NoAttemptsRemaining),

                _ => SubmitVerificationAttemptResult.Fail(
                        SubmitVerificationAttemptOutcome.AlreadyVerified),
            };
        }

        /// The identity attempt budget. Reuses the existing configurable setting rather than
        /// introducing a competing one; the contract's default is the same value.
        private async Task<int> ResolveMaxAttemptsAsync()
        {
            var settings = await _systemSettingsRepository.GetAsync();

            var configured = settings?.max_liveness_attempts ?? 0;

            return configured > 0 ? configured : DefaultMaxAttempts;
        }

        /// Null when unconfigured, or configured outside the cosine range - both are treated
        /// as "not set" so a typo cannot silently become an accept-everything threshold.
        private async Task<double?> ResolveThresholdAsync()
        {
            var settings = await _systemSettingsRepository.GetAsync();

            var threshold = settings?.sface_cosine_threshold;

            if (threshold is not { } value || !double.IsFinite(value) || value <= 0d || value > 1d)
                return null;

            return value;
        }

        private static VerificationSessionResponse BuildSessionResponse(
            VerificationSessionView session, int examSessionId) =>
            new()
            {
                VerificationSessionId = session.PublicId,
                ExamSessionId = examSessionId,
                Policy = new VerificationPolicyDto
                {
                    MaxAttempts = session.MaxAttempts,
                    AttemptsUsed = session.AttemptsUsed,
                    RequiredBlinks = LivenessPolicy.RequiredBlinks,

                    // Locked false for v1: no camera frame leaves the student's device.
                    RequiresSnapshotOnFailure = false,

                    EmbeddingModel = FaceEmbedding.Model,
                    EmbeddingVersion = FaceEmbedding.ModelVersion,
                },
            };

        private static SubmitVerificationAttemptResponse BuildAttemptResponse(
            VerificationAttemptView attempt) =>
            new()
            {
                Outcome = attempt.Outcome.ToString(),
                AttemptNumber = attempt.AttemptNumber,
                AttemptsRemaining = attempt.AttemptsRemainingAfter,
                MatchScore = attempt.MatchScore,
            };
    }
}
