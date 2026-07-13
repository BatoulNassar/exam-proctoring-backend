using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class StudentAnswer : BaseEntity
    {
        public int student_session_id { get; set; }
        public int question_id { get; set; }
        public string student_response { get; set; }
        public DateTime saved_at { get; set; }

        public Question Question { get; set; }
        public StudentSession StudentSession { get; set; }

    }
}
