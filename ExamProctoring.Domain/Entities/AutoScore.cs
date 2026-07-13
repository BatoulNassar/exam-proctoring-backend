using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class AutoScore : BaseEntity
    {
        public int student_session_id { get; set; }
        public int question_id { get; set; }
        public string student_answer { get; set; }
        public string correct_answer { get; set; }
        public int marks_awarded { get; set; }
        public int max_marks { get; set; }
       
        public Question Question { get; set; }
        public StudentSession StudentSession { get; set; }

    } 
}
