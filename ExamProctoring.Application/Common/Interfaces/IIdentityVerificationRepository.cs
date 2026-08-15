using ExamProctoring.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// Read-only projection of one verification session plus the assignment it belongs to.
    /// Flat on purpose: no navigation collections, and never the reference embedding.
    public class VerificationSessionView
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; }
        public int StudentSessionId { get; set; }
        public int ExamSessionId { get; set; }
        public int StudentId { get; set; }
        public IdentityVerificationStatus Status { get; set; }
        public int AttemptsUsed { get; set; }
        public int MaxAttempts { get; set; }
        public DateTime? VerifiedAtUtc { get; set; }

        public bool IsVerified => VerifiedAtUtc.HasValue;
        public int AttemptsRemaining => Math.Max(0, MaxAttempts - AttemptsUsed);
    }

    /// A previously recorded attempt, sufficient to rebuild its original response verbatim.
    public class VerificationAttemptView
    {
        public IdentityVerificationOutcome Outcome { get; set; }
        public int AttemptNumber { get; set; }
        public int AttemptsRemainingAfter { get; set; }
        public double? MatchScore { get; set; }
    }

    /// The trusted reference identity for one student. Carries the vector in its stored form;
    /// it is decoded, compared and discarded inside the Application service and never leaves it.
    public class ReferenceFaceView
    {
        public byte[]? Embedding { get; set; }
        public string? Model { get; set; }
        public string? ModelVersion { get; set; }

        public bool IsEnrolled => Embedding is { Length: > 0 };
    }

    /// Everything needed to durably record one attempt. Assembled by the Application service
    /// after it has decided the outcome; the repository makes no business decision, it only
    /// applies this atomically.
    public class RecordAttemptCommand
    {
        public int VerificationSessionId { get; set; }
        public int StudentSessionId { get; set; }
        public string ClientAttemptId { get; set; } = string.Empty;

        public IdentityVerificationOutcome Outcome { get; set; }

        /// False for outcomes that ran no comparison, so a student is never charged an attempt
        /// for a condition they cannot influence.
        public bool ConsumesAttempt { get; set; }

        public double? MatchScore { get; set; }
        public double? ThresholdUsed { get; set; }

        public bool LivenessAccepted { get; set; }
        public int LivenessBlinkCount { get; set; }
        public int LivenessFramesAnalysed { get; set; }
        public int LivenessDurationMs { get; set; }
        public double LivenessMinEyeOpenness { get; set; }
        public double LivenessMaxEyeOpenness { get; set; }
        public string? LivenessRejectionReason { get; set; }

        public string EmbeddingModel { get; set; } = string.Empty;
        public string EmbeddingModelVersion { get; set; } = string.Empty;

        public DateTime? CapturedAtUtc { get; set; }
        public DateTime NowUtc { get; set; }
    }

    public enum RecordAttemptStatus
    {
        /// Persisted for the first time.
        Recorded,

        /// This clientAttemptId was already recorded; the original is returned untouched.
        Replayed,

        /// The budget was exhausted before this attempt could be consumed.
        NoAttemptsRemaining,

        /// The session was verified by a concurrent attempt.
        AlreadyVerified,
    }

    public class RecordAttemptResult
    {
        public RecordAttemptStatus Status { get; set; }

        /// Set for Recorded and Replayed.
        public VerificationAttemptView? Attempt { get; set; }
    }

    /// Persistence for identity verification. Every method that mutates state does so through
    /// database constraints and conditional updates rather than read-then-write, because two
    /// clients retrying over a weak connection is the normal case here, not the exception.
    public interface IIdentityVerificationRepository
    {
        /// The verification session for one exam assignment, or null if none exists yet.
        Task<VerificationSessionView?> GetByStudentSessionAsync(int studentSessionId);

        /// Creates the verification session for an assignment. Safe under concurrency: if a
        /// parallel request created it first, that row is returned instead of failing, so two
        /// simultaneous opens of the client cannot mint two budgets.
        ///
        /// WasCreated distinguishes the two: it is false for the caller that lost the race, so
        /// that caller reports 200 (resumed) rather than 201, and only one request in a race
        /// ever claims to have created the session.
        Task<(VerificationSessionView Session, bool WasCreated)> CreateAsync(
            int studentSessionId, int maxAttempts, string? deviceId, DateTime nowUtc);

        /// Looks a verification session up by its student-facing id, enforcing ownership inside
        /// the query. A foreign session returns null rather than being fetched and compared.
        Task<VerificationSessionView?> GetByPublicIdAsync(int studentId, Guid publicId);

        /// The student's trusted reference identity. Returns a view with IsEnrolled false when
        /// no reference has been imported.
        Task<ReferenceFaceView?> GetReferenceFaceAsync(int studentId);

        /// Fast-path idempotency read: has this clientAttemptId already been recorded?
        Task<VerificationAttemptView?> FindAttemptAsync(int verificationSessionId, string clientAttemptId);

        /// Records one attempt, consuming budget and - on a match - marking both the
        /// verification session and the StudentSession verified, all in a single transaction.
        ///
        /// Consuming the budget before claiming the clientAttemptId is deliberate: it makes the
        /// session row the contention point, so a duplicate key rolls the consumption back
        /// rather than leaving a student charged for an attempt that was never recorded.
        Task<RecordAttemptResult> RecordAttemptAsync(RecordAttemptCommand command);
    }
}
