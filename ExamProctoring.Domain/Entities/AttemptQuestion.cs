using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;

namespace ExamProctoring.Domain.Entities
{
    /// One question as it was served to one student, in that student's personalised order.
    /// Materialised once at first start and never regenerated, so a resume returns exactly
    /// the paper the student originally received even if the question bank changes afterwards.
    ///
    /// stem, type and marks are deliberately snapshotted rather than read through the
    /// Question navigation: that is what makes the paper reproducible, and it also keeps
    /// Question.correct_answer off the student read path entirely.
    public class AttemptQuestion : BaseEntity
    {
        public int student_session_id { get; set; }

        /// Provenance back to the authored question. Used by grading, never by the student API.
        public int question_id { get; set; }

        /// Student-facing opaque question identifier. The authored Question.id is never exposed.
        public Guid public_id { get; set; }

        /// 1-based presentation order for this student.
        public int ordinal { get; set; }

        public QuestionType type { get; set; }

        /// Snapshot of Question.question_text at materialisation time.
        public string stem { get; set; } = string.Empty;

        /// Snapshot of Question.marks at materialisation time.
        public int marks { get; set; }

        public StudentSession StudentSession { get; set; }
        public Question Question { get; set; }
        public ICollection<AttemptQuestionOption> Options { get; set; } = new List<AttemptQuestionOption>();
    }
}
