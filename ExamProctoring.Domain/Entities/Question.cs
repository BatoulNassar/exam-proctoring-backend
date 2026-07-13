using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class Question : BaseEntity
    {
        public int question_bank_id { get; set; }
        public QuestionType type { get; set; }
        public string question_text { get; set; }        
        public string? option_a { get; set; }
        public string? option_b { get; set; }
        public string? option_c { get; set; }
        public string? option_d { get; set; }
        public string? option_e { get; set; }
        public string correct_answer { get; set; }
        public int marks { get; set; }

        public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
        public QuestionBank QuestionBank { get; set; }
        public ICollection<AutoScore> AutoScores { get; set; } = new List<AutoScore>();
    }
}
