using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    /// The student's single current answer to one question of their attempt.
    ///
    /// There is exactly one row per (student_session_id, question_id) - the unique index is the
    /// upsert key, and repeated saves overwrite in place. No answer history is kept: the
    /// contract specifies last-accepted-write-wins by server receipt time.
    public class StudentAnswer : BaseEntity
    {
        public int student_session_id { get; set; }
        public int question_id { get; set; }

        /// Canonical JSON produced by AnswerValueCodec, e.g.
        /// {"type":"MCQ_SINGLE","optionIds":["..."]} or {"type":"ESSAY","text":"..."}.
        /// Option ids are the per-attempt AttemptQuestionOption.public_id values the student
        /// actually submitted - never a storage slot letter, and never any answer-key data.
        public string student_response { get; set; }

        /// Server receipt time of the accepted write. Authoritative for "last write wins".
        public DateTime saved_at { get; set; }

        /// Cumulative focused milliseconds the client attributes to this question, clamped to
        /// the attempt span. Telemetry only - never used for grading, eligibility or deadlines.
        public int duration_ms { get; set; }

        /// The student's device clock at the time of answering. Untrusted, audit only; it never
        /// orders writes and never extends the deadline.
        public DateTime? client_answered_at { get; set; }

        public Question Question { get; set; }
        public StudentSession StudentSession { get; set; }

    }
}
