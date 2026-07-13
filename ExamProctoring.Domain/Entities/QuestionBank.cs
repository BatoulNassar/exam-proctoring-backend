using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class QuestionBank : BaseEntity
    {
        public string title { get; set; }
        public string course_code { get; set; }
        public QuestionBankStatus status { get; set; }
        public string version { get; set; }
        public int authored_by_admin_id { get; set; }
        public DateTime?locked_at { get; set; }
        public bool randomization { get; set; }
        public bool option_shuffle { get; set; }

        public User Admin { get; set; }
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<ExamSession> ExamSessions{ get; set; } = new List<ExamSession>();
    }
} 
