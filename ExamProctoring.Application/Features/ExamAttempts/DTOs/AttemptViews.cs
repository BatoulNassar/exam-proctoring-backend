using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;

namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// Flat read-only projection of one StudentSession joined to its ExamSession and question
    /// bank configuration. Carries only the columns the attempt decisions need; no navigation
    /// collections (answers, alerts, monitoring, questions) are loaded.
    ///
    /// Mirrors the StudentAssignmentView pattern used by eligibility.
    public class AttemptView
    {
        // ----- attempt (StudentSession) -----
        public int StudentSessionId { get; set; }
        public Guid PublicId { get; set; }
        public StudentSessionStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public string? DeviceId { get; set; }
        public int? QuestionCount { get; set; }
        public DateTime? SubmittedAt { get; set; }

        // ----- frozen terminal result -----
        public DateTime? FinalisedAt { get; set; }
        public AttemptFinalisationReason? FinalisationReason { get; set; }
        public int? AnsweredCount { get; set; }
        public string? ReceiptCode { get; set; }

        // ----- exam session -----
        public int ExamSessionId { get; set; }
        public ExamSessionStatus ExamSessionStatus { get; set; }
        public string CourseTag { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public int ExtendedByMinutes { get; set; }
        public int LoginWindowMinutes { get; set; }
        public DateTime? GracePeriodEndedAt { get; set; }
        public int EyeGazeThresholdSec { get; set; }

        // ----- question bank configuration -----
        public int QuestionBankId { get; set; }
        public bool Randomization { get; set; }
        public bool OptionShuffle { get; set; }

        public bool HasStarted => StartedAt.HasValue;

        /// finalised_at is checked first because it is the authoritative terminal marker written
        /// by the shared finalisation path; the status and submitted_at checks remain so rows
        /// that predate Phase 3 are still treated as terminal.
        public bool IsFinalised =>
            FinalisedAt.HasValue
            || SubmittedAt.HasValue
            || Status == StudentSessionStatus.Submitted
            || Status == StudentSessionStatus.Terminated;
    }

    /// One authored question, projected for materialisation.
    ///
    /// SECURITY: correct_answer is deliberately absent. This type is the only shape in which
    /// authored questions reach the attempt code, so the answer key is never fetched from the
    /// database on any student-triggered path.
    public class QuestionSourceView
    {
        public int QuestionId { get; set; }
        public QuestionType Type { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int Marks { get; set; }
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? OptionE { get; set; }
    }

    /// One already-materialised question, projected for the student read path.
    /// source_slot is intentionally not projected.
    public class AttemptQuestionView
    {
        public Guid PublicId { get; set; }
        public int Ordinal { get; set; }
        public QuestionType Type { get; set; }
        public string Stem { get; set; } = string.Empty;
        public int Marks { get; set; }
        public List<AttemptQuestionOptionView> Options { get; set; } = new();
    }

    public class AttemptQuestionOptionView
    {
        public Guid PublicId { get; set; }
        public int Ordinal { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    /// One materialised question resolved as the target of an answer write.
    ///
    /// Carries the option ids that are legal for THIS question, so option validation is done
    /// against the student's own materialised paper rather than against Question.option_a..e.
    /// source_slot is deliberately not projected.
    public class AttemptQuestionTargetView
    {
        public int AttemptQuestionId { get; set; }

        /// The authored question - the StudentAnswer upsert key.
        public int QuestionId { get; set; }

        public Guid PublicId { get; set; }
        public QuestionType Type { get; set; }
        public List<Guid> OptionPublicIds { get; set; } = new();
    }
}
