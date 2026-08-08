using ExamProctoring.Domain.Common;
using System;

namespace ExamProctoring.Domain.Entities
{
    /// One selectable option as it was served to one student, in that student's option order.
    ///
    /// public_id is an opaque per-attempt UUID rather than the storage slot letter. That is
    /// what makes option shuffling meaningful: if the identifier were "a".."e", two students
    /// could still share "the answer is c" even though their screens disagree.
    /// source_slot is the internal mapping back to Question.option_a..option_e and must never
    /// leave the server.
    public class AttemptQuestionOption : BaseEntity
    {
        public int attempt_question_id { get; set; }

        /// Student-facing opaque option identifier, submitted back on an answer write.
        public Guid public_id { get; set; }

        /// 1-based presentation order for this student.
        public int ordinal { get; set; }

        /// Internal slot letter ("a".."e") identifying which Question column this came from.
        /// Server-side only - never projected into any student-facing DTO.
        public string source_slot { get; set; } = string.Empty;

        /// Snapshot of the option text at materialisation time.
        public string label { get; set; } = string.Empty;

        public AttemptQuestion AttemptQuestion { get; set; }
    }
}
