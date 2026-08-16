using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// Persistence for one-time auto-grading of a finalised attempt.
    ///
    /// Deliberately separate from IAttemptRepository. That interface documents that the
    /// authored Question row - and therefore correct_answer - is never loaded on a
    /// student-triggered path, and that guarantee is worth keeping literally true. Grading is
    /// the one operation that legitimately needs the key, so it gets its own narrow seam whose
    /// every projection is server-side only.
    public interface IAttemptGradingRepository
    {
        /// Everything needed to score an attempt: the frozen paper, each option's source slot,
        /// the authored answer key, and the saved answers.
        ///
        /// SERVER-ONLY. Nothing in the returned graph may be projected into any student
        /// response. Returns null when the attempt has no materialised paper.
        Task<GradingSourceView?> GetGradingSourceAsync(int studentSessionId);

        /// The frozen grading facts for an already-graded attempt, reconstructed WITHOUT
        /// touching the question bank: the paper, the saved answers and the AutoScore rows are
        /// all immutable once the attempt is finalised, so a replay months later returns the
        /// same numbers even if the bank has since been edited.
        ///
        /// Returns null when the attempt has not been graded yet.
        Task<FrozenGradingView?> GetFrozenGradingAsync(int studentSessionId);

        /// Persists the AutoScore rows and claims the one-time grading marker in a SINGLE
        /// transaction.
        ///
        /// The claim is a conditional UPDATE on graded_at_utc IS NULL, so two concurrent
        /// finalisation paths cannot both grade: the loser's inserts roll back with its claim
        /// and it replays the winner's snapshot. A crash mid-way leaves graded_at_utc null and
        /// no scores, which a later retry grades cleanly.
        Task<GradingPersistOutcome> TryPersistAsync(GradingPersistCommand command);
    }

    /// SERVER-ONLY. Carries the answer key and option source slots.
    public sealed class GradingSourceView
    {
        public List<GradingQuestionSourceView> Questions { get; set; } = new();
    }

    /// SERVER-ONLY. One materialised question with the data needed to score it.
    public sealed class GradingQuestionSourceView
    {
        /// Authored question id - the AutoScore key. Never exposed to a student.
        public int QuestionId { get; set; }

        /// Student-facing question id, safe to return.
        public Guid PublicId { get; set; }

        public int Ordinal { get; set; }
        public QuestionType Type { get; set; }

        /// Frozen marks from the materialised paper, never the live bank value.
        public int Marks { get; set; }

        /// SERVER-ONLY. Authored key: a slot letter, comma-separated slot letters, or
        /// TRUE/FALSE. Never logged, never projected.
        public string? CorrectAnswer { get; set; }

        /// SERVER-ONLY. The student's option ids mapped to their source slots.
        public List<GradingOptionSourceView> Options { get; set; } = new();

        /// Canonical stored answer, or null when the student never saved one.
        public string? StoredResponse { get; set; }
    }

    /// SERVER-ONLY. Maps a student-facing option id back to its authored slot.
    public sealed class GradingOptionSourceView
    {
        public Guid PublicId { get; set; }

        /// "a".."e". Never leaves the server.
        public string SourceSlot { get; set; } = string.Empty;

        /// Needed only to resolve a TRUE/FALSE key, which names the option rather than a slot.
        public string Label { get; set; } = string.Empty;
    }

    /// The frozen result of a completed grading pass, safe to project into a response.
    public sealed class FrozenGradingView
    {
        public DateTime GradedAtUtc { get; set; }

        /// Paper items in frozen ordinal order.
        public List<FrozenGradingQuestionView> Questions { get; set; } = new();
    }

    public sealed class FrozenGradingQuestionView
    {
        public Guid PublicId { get; set; }
        public int Ordinal { get; set; }
        public QuestionType Type { get; set; }
        public int Marks { get; set; }

        /// Null for manual questions, which have no AutoScore row.
        public int? MarksAwarded { get; set; }

        /// True when the student had a non-empty saved answer at finalisation.
        public bool WasAnswered { get; set; }
    }

    /// One auto-graded question's persisted score.
    public sealed class AutoScoreRecord
    {
        public int QuestionId { get; set; }
        public int MarksAwarded { get; set; }
        public int MaxMarks { get; set; }

        /// The student's selection as slot letters, or empty when unanswered.
        /// SERVER-ONLY diagnostic; never returned.
        public string StudentAnswer { get; set; } = string.Empty;

        /// SERVER-ONLY. Stored so a score stays explainable after the bank changes.
        public string CorrectAnswer { get; set; } = string.Empty;
    }

    public sealed class GradingPersistCommand
    {
        public int StudentSessionId { get; set; }
        public DateTime NowUtc { get; set; }

        /// Written to StudentSession.awarded_marks. Auto marks only.
        public int CurrentGrade { get; set; }

        public List<AutoScoreRecord> Scores { get; set; } = new();
    }

    public enum GradingPersistOutcome
    {
        /// This caller graded the attempt.
        Persisted,

        /// Another path graded it first; its snapshot is authoritative.
        AlreadyGraded,
    }
}
