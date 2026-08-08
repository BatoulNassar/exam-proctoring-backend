using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// Persistence for the one-time terminal transition of an attempt.
    ///
    /// Finalisation is guarded by a single conditional UPDATE on finalised_at, which is what
    /// makes student submit, automatic expiry and proctor termination safe to race against each
    /// other: exactly one of them can claim the transition, and every loser reads the winner's
    /// frozen snapshot instead of producing a second result.
    public interface IAttemptFinalisationRepository
    {
        /// The frozen terminal state, or null while the attempt is still open.
        Task<AttemptFinalSnapshot?> GetSnapshotAsync(int studentSessionId);

        /// Claims the terminal transition. The StudentSession fields and the audit row commit in
        /// ONE transaction, so a crash can never leave a receipt without a finalised_at or
        /// frozen counts without the status behind them.
        ///
        /// Idempotency records are written separately and always AFTER this succeeds, which is
        /// the safe ordering: a crash can lose a record (a retry then re-derives the same frozen
        /// response) but can never record a success that was not finalised.
        Task<FinalisationPersistResult> FinaliseAsync(FinalisationCommand command);

        /// Attempts that are still running but whose personal deadline has passed.
        /// Used by the janitor; never by the request path, which enforces the deadline directly.
        Task<List<ExpiredAttemptView>> GetExpiredAttemptsAsync(DateTime nowUtc, int batchSize);
    }

    /// The immutable terminal facts about one attempt.
    public class AttemptFinalSnapshot
    {
        public Guid AttemptPublicId { get; set; }
        public StudentSessionStatus Status { get; set; }
        public AttemptFinalisationReason Reason { get; set; }
        public DateTime FinalisedAtUtc { get; set; }
        public int AnsweredCount { get; set; }
        public int QuestionCount { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
    }

    /// One attempt the janitor should finalise.
    public class ExpiredAttemptView
    {
        public int StudentSessionId { get; set; }
        public int ExamSessionId { get; set; }
        public string CourseTag { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
    }

    /// Everything the terminal transition needs. Assembled by the finalisation service, so the
    /// repository makes no business decisions - it only enforces the one-time guard.
    public class FinalisationCommand
    {
        public int StudentSessionId { get; set; }
        public int ExamSessionId { get; set; }

        public AttemptFinalisationReason Reason { get; set; }

        /// The persisted status to move to. Terminated is never downgraded to Submitted.
        public StudentSessionStatus TargetStatus { get; set; }

        public DateTime NowUtc { get; set; }

        /// Set only for a real student/client submission. Left null for automatic expiry and
        /// proctor termination, where nobody submitted anything.
        public DateTime? SubmittedAtUtc { get; set; }

        public int AnsweredCount { get; set; }
        public int QuestionCount { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;

        // ----- audit, written inside the same transaction -----
        public int AuditActorId { get; set; }
        public string AuditActorType { get; set; } = string.Empty;
        public string AuditAction { get; set; } = string.Empty;
        public string AuditDetails { get; set; } = string.Empty;
    }

    public enum FinalisationOutcome
    {
        /// This caller claimed the terminal transition.
        Finalised,

        /// Another path had already finalised it; Snapshot carries the winning result.
        AlreadyFinalised,
    }

    public class FinalisationPersistResult
    {
        public FinalisationOutcome Outcome { get; init; }
        public AttemptFinalSnapshot? Snapshot { get; init; }

        public static FinalisationPersistResult Finalised(AttemptFinalSnapshot snapshot) =>
            new() { Outcome = FinalisationOutcome.Finalised, Snapshot = snapshot };

        public static FinalisationPersistResult AlreadyFinalised(AttemptFinalSnapshot snapshot) =>
            new() { Outcome = FinalisationOutcome.AlreadyFinalised, Snapshot = snapshot };
    }
}
