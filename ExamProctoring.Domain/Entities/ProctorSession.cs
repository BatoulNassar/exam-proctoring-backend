using ExamProctoring.Domain.Common;
using System;

namespace ExamProctoring.Domain.Entities
{
    public class ProctorSession : BaseEntity
    {
        public int exam_session_id { get; set; }
        public int proctor_id { get; set; }

        public ExamSession ExamSession { get; set; }
        public User Proctor { get; set; }
    }
}
