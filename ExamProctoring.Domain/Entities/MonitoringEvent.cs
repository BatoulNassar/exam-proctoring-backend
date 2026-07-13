using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class MonitoringEvent : BaseEntity
    {
        public int student_session_id { get; set; }
        public string event_type { get; set; }
        public string event_details { get; set; }
        public DateTime occured_at { get; set; }

        public ICollection<AlertEvent> AlertEvents { get; set; } = new List<AlertEvent>();
        public StudentSession StudentSession { get; set; }
    }
} 
