using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    public class AttemptFinalisationService : IAttemptFinalisationService
    {
        /// A collision across 32^8 possibilities is astronomically unlikely, but receipt_code is
        /// uniquely indexed, so the one recoverable failure is retried rather than surfaced to a
        /// student who has just finished their exam.
        private const int ReceiptAttempts = 5;

        private readonly IAttemptFinalisationRepository _finalisationRepository;
        private readonly IStudentAnswerRepository _studentAnswerRepository;
        private readonly ILogger<AttemptFinalisationService> _logger;

        public AttemptFinalisationService(
            IAttemptFinalisationRepository finalisationRepository,
            IStudentAnswerRepository studentAnswerRepository,
            ILogger<AttemptFinalisationService> logger)
        {
            _finalisationRepository = finalisationRepository;
            _studentAnswerRepository = studentAnswerRepository;
            _logger = logger;
        }

        public async Task<AttemptFinalisationOutcome> FinaliseAsync(AttemptFinalisationContext context)
        {
            var nowUtc = DateTime.UtcNow;

            // Fast path: another trigger already finalised this attempt, and its frozen snapshot
            // is the answer for everyone from now on.
            var existing = await _finalisationRepository.GetSnapshotAsync(context.StudentSessionId);
            if (existing != null)
                return AlreadyFinalised(existing);

            var answeredCount = await CountAnsweredAsync(context.StudentSessionId);

            // A terminated attempt is final: finalising it later must never relabel it submitted.
            var targetStatus = context.IsAlreadyTerminated
                               || context.Reason == AttemptFinalisationReason.ProctorTerminated
                ? StudentSessionStatus.Terminated
                : StudentSessionStatus.Submitted;

            // submitted_at means "the student submitted", so it stays null when nobody did -
            // automatic expiry and proctor termination use finalised_at only.
            var submittedAtUtc = context.Reason is AttemptFinalisationReason.StudentSubmit
                                 or AttemptFinalisationReason.ClientTimerExpired
                                 or AttemptFinalisationReason.ConnectivityRecovery
                ? nowUtc
                : (DateTime?)null;

            for (var attempt = 1; attempt <= ReceiptAttempts; attempt++)
            {
                var command = new FinalisationCommand
                {
                    StudentSessionId = context.StudentSessionId,
                    ExamSessionId = context.ExamSessionId,
                    Reason = context.Reason,
                    TargetStatus = targetStatus,
                    NowUtc = nowUtc,
                    SubmittedAtUtc = submittedAtUtc,
                    AnsweredCount = answeredCount,
                    QuestionCount = context.QuestionCount,
                    ReceiptCode = ReceiptCodeGenerator.Generate(context.CourseTag),

                    AuditActorId = context.ActorId,
                    AuditActorType = context.ActorType,
                    AuditAction = AuditActionFor(context.Reason),

                    // Never answer content, never a device id, never a token - only the counts and
                    // the reason, which is what an integrity review actually needs.
                    AuditDetails =
                        $"Attempt finalised: reason={context.Reason}, status={targetStatus}, " +
                        $"answered={answeredCount}/{context.QuestionCount}",
                };

                try
                {
                    var result = await _finalisationRepository.FinaliseAsync(command);

                    if (result.Outcome == FinalisationOutcome.Finalised)
                    {
                        _logger.LogInformation(
                            "Attempt {StudentSessionId} finalised: reason={Reason} status={Status} answered={Answered}/{Total}.",
                            context.StudentSessionId, context.Reason, targetStatus,
                            answeredCount, context.QuestionCount);

                        return new AttemptFinalisationOutcome
                        {
                            Status = AttemptFinalisationStatus.Finalised,
                            Snapshot = result.Snapshot!,
                        };
                    }

                    // Lost the claim between the check above and the conditional update. The
                    // winner's snapshot is authoritative; nothing of ours was written.
                    return AlreadyFinalised(result.Snapshot!);
                }
                catch (ReceiptCodeCollisionException) when (attempt < ReceiptAttempts)
                {
                    _logger.LogWarning(
                        "Receipt code collision finalising attempt {StudentSessionId}; regenerating (attempt {Attempt}).",
                        context.StudentSessionId, attempt);
                }
            }

            throw new InvalidOperationException(
                $"Could not generate a unique receipt code for attempt {context.StudentSessionId}.");
        }

        private static AttemptFinalisationOutcome AlreadyFinalised(AttemptFinalSnapshot snapshot) =>
            new() { Status = AttemptFinalisationStatus.AlreadyFinalised, Snapshot = snapshot };

        /// A persisted answer row does not by itself mean the question was answered: a cleared
        /// answer is stored deliberately so the client can distinguish "cleared" from "never
        /// touched". Only semantically non-empty answers count, decided by the codec rather than
        /// by pattern-matching the stored JSON.
        private async Task<int> CountAnsweredAsync(int studentSessionId)
        {
            var answers = await _studentAnswerRepository.GetForAttemptAsync(studentSessionId);

            return answers.Count(a => !AnswerValueCodec.IsCleared(
                AnswerValueCodec.Decode(a.StoredResponse)));
        }

        private static string AuditActionFor(AttemptFinalisationReason reason) => reason switch
        {
            AttemptFinalisationReason.ProctorTerminated => "AttemptTerminated",
            AttemptFinalisationReason.ServerExpiry => "AttemptExpired",
            _ => "AttemptSubmitted",
        };
    }

    /// Raised by the repository when the generated receipt code already exists, so the service can
    /// regenerate. Not a condition worth surfacing to a caller.
    public class ReceiptCodeCollisionException : Exception
    {
        public ReceiptCodeCollisionException(string message) : base(message) { }
    }

    /// Endpoint discriminators recorded on idempotency records, so one key can never be shared
    /// between the answer and submit endpoints.
    public static class ExamAttemptEndpoints
    {
        public const string UpsertAnswer = "PUT_ANSWER";
        public const string SubmitAttempt = "SUBMIT_ATTEMPT";
    }
}
