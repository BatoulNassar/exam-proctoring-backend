using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class ProctorAction : BaseEntity
    {
        public int alert_event_id {  get; set; }
        public int admin_id { get; set; }
        public string action_type { get; set; }
        public string action_note { get; set; }
        public DateTime acted_at { get; set; }

        public User Admin { get; set; }
        public AlertEvent AlertEvent { get; set; } 
        public ICollection<WarningMessage> WarningMessages { get; set; } = new List<WarningMessage>();
    }
}
