using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class WarningMessage : BaseEntity
    {
        public int proctor_action_id { get; set; }
        public int student_session_id { get; set; }
        public string message_text { get; set; }
        public DateTime sent_at { get; set; } 
        public DateTime?acknowledged_at { get; set; }

        public StudentSession StudentSession { get; set; }
        public ProctorAction ProctorAction { get; set; }
    }
}
