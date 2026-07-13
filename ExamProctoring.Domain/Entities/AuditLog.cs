using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class AuditLog : BaseEntity
    {
        public int exam_session_id { get; set; }
        public int actor_id { get; set; }
        public string actor_type { get; set; }
        public string action { get; set; }
        public int entity_id { get; set; }
        public string entity_type { get; set; }
        public string details { get; set; }
        public DateTime occurred_at { get; set; }

        public ExamSession ExamSession { get; set; }
    }
} 
