using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// The student-facing grading snapshot returned inside the submit receipt.
    ///
    /// Everything here is derived from the attempt's own frozen state - the materialised paper,
    /// the saved answers and the persisted AutoScore rows. Nothing in this graph carries an
    /// answer key, an option source slot, a selected option id, or any text the student wrote:
    /// the receipt tells a student what they scored, not what the answers were.
    public sealed class GradingSnapshotDto
    {
        /// AUTO_COMPLETE when the paper has no manual questions, otherwise PENDING_MANUAL.
        /// COMPLETE is reserved for a future results API and is never sent from submit.
        public string Status { get; set; } = string.Empty;

        /// Server instant auto-scoring ran. Frozen; identical on every retry.
        public System.DateTime AutoGradedAtUtc { get; set; }

        public GradingSummaryDto Summary { get; set; } = new();

        /// Always present, with zeros when the paper has no auto questions.
        public GradingAutoDto Auto { get; set; } = new();

        /// Always present, with zeros when the paper has no manual questions.
        public GradingManualDto Manual { get; set; } = new();

        /// One row per materialised question, in the same frozen ordinal order as GET questions.
        public List<GradingQuestionDto> Questions { get; set; } = new();
    }

    /// The four integers the receipt screen shows in large type.
    public sealed class GradingSummaryDto
    {
        /// Marks already awarded. At submit this is auto marks only - never a guessed manual total.
        public int CurrentGrade { get; set; }

        /// Sum of marks across the whole materialised paper.
        public int ExamMaxMarks { get; set; }

        /// Lowest achievable final total: currentGrade, since every pending manual question
        /// could still score zero.
        public int PossibleFinalMin { get; set; }

        /// Highest achievable final total: currentGrade + every pending manual mark.
        public int PossibleFinalMax { get; set; }
    }

    /// MCQ_SINGLE + MCQ_MULTI + TRUE_FALSE. Scored once at finalisation and never re-marked.
    public sealed class GradingAutoDto
    {
        public int AwardedMarks { get; set; }
        public int MaxMarks { get; set; }

        /// MCQ_SINGLE + MCQ_MULTI only.
        public int McqAwardedMarks { get; set; }
        public int McqMaxMarks { get; set; }

        public int TrueFalseAwardedMarks { get; set; }
        public int TrueFalseMaxMarks { get; set; }

        public int QuestionCount { get; set; }

        /// Auto questions with a non-empty saved answer.
        public int AnsweredCount { get; set; }

        /// Auto questions awarded full marks.
        public int CorrectCount { get; set; }
    }

    /// SHORT_ANSWER + ESSAY. Not graded here; a professor marks them later.
    public sealed class GradingManualDto
    {
        /// Null whenever marks are still pending, 0 when the paper has no manual questions.
        /// Never a guessed number - serialized even when null, because the client branches on it.
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int? AwardedMarks { get; set; }

        /// Marks still awaiting a human. Unanswered manual questions still count: the professor
        /// has a blank row to mark, typically zero.
        public int PendingMaxMarks { get; set; }

        public int QuestionCount { get; set; }
        public int AnsweredCount { get; set; }
        public int UnansweredCount { get; set; }
    }

    /// One paper item's outcome. Carries no stem, no options and no submitted answer.
    public sealed class GradingQuestionDto
    {
        /// The same public UUID GET questions exposed. Internal integer ids never appear.
        public System.Guid QuestionId { get; set; }

        public int Ordinal { get; set; }

        /// MCQ_SINGLE | MCQ_MULTI | TRUE_FALSE | SHORT_ANSWER | ESSAY
        public string Type { get; set; } = string.Empty;

        public int MaxMarks { get; set; }

        /// AUTO | MANUAL
        public string GradingMethod { get; set; } = string.Empty;

        /// CORRECT | INCORRECT | UNANSWERED for AUTO, PENDING_MANUAL for MANUAL.
        public string Result { get; set; } = string.Empty;

        /// An integer for every AUTO row including zero; null for every MANUAL row at submit.
        /// Serialized even when null so the client can distinguish "pending" from "zero".
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int? MarksAwarded { get; set; }
    }

    /// Stable contract vocabulary. These strings are part of the client contract.
    public static class GradingStatuses
    {
        public const string AutoComplete = "AUTO_COMPLETE";
        public const string PendingManual = "PENDING_MANUAL";
    }

    public static class GradingMethods
    {
        public const string Auto = "AUTO";
        public const string Manual = "MANUAL";
    }

    public static class GradingResults
    {
        public const string Correct = "CORRECT";
        public const string Incorrect = "INCORRECT";
        public const string Unanswered = "UNANSWERED";
        public const string PendingManual = "PENDING_MANUAL";
    }
}
