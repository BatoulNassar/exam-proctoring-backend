using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Enums;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    /// The single path by which an attempt reaches its terminal state.
    ///
    /// Student submit, automatic server expiry and proctor termination all go through here, so
    /// there is exactly one implementation of the terminal guard, the answered-count rule, the
    /// receipt, the audit entry and the frozen snapshot. Three near-identical copies would drift,
    /// and the drift would only show up as inconsistent receipts after an exam had been sat.
    ///
    /// Deliberately knows nothing about idempotency keys or HTTP: finalisation is already
    /// idempotent because finalised_at is written exactly once. The submit endpoint layers key
    /// handling on top for response replay.
    public interface IAttemptFinalisationService
    {
        /// Finalises the attempt, or returns the existing frozen result when another trigger got
        /// there first. Never rewrites a terminal state.
        Task<AttemptFinalisationOutcome> FinaliseAsync(AttemptFinalisationContext context);
    }

    public class AttemptFinalisationContext
    {
        public int StudentSessionId { get; set; }
        public int ExamSessionId { get; set; }
        public string CourseTag { get; set; } = string.Empty;

        /// Frozen question count from Start. Used as-is; never recomputed from the bank.
        public int QuestionCount { get; set; }

        public AttemptFinalisationReason Reason { get; set; }

        /// True when the attempt is already persisted as Terminated, so the terminal status is
        /// preserved rather than downgraded to Submitted.
        public bool IsAlreadyTerminated { get; set; }

        public int ActorId { get; set; }

        /// "Student" | "System" | "Admin"
        public string ActorType { get; set; } = string.Empty;
    }

    public enum AttemptFinalisationStatus
    {
        /// This call claimed the terminal transition.
        Finalised,

        /// It was already terminal; Snapshot carries the frozen result.
        AlreadyFinalised,
    }

    public class AttemptFinalisationOutcome
    {
        public AttemptFinalisationStatus Status { get; init; }
        public AttemptFinalSnapshot Snapshot { get; init; } = new();

        /// The attempt's frozen grading snapshot, or null when grading did not complete.
        ///
        /// Nullable on purpose. Finalisation and grading are separate commits, and a grading
        /// failure must not undo a finalisation that already succeeded - the student's answers
        /// are safely frozen either way. Callers decide what a null means for them: the submit
        /// endpoint refuses to return a receipt without it, while the expiry janitor and proctor
        /// termination log it and move on, because a later submit retry will grade the attempt.
        public DTOs.GradingSnapshotDto? Grading { get; init; }
    }
}
