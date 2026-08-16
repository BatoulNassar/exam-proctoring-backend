using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class AlertEvent : BaseEntity
    {
        public int student_session_id { get; set; }
        public int monitoring_event_id { get; set; }
        public string alert_type { get; set; }
        public AlertSeverity severity { get; set; }
        public AlertStatus status { get; set; }
        public DateTime triggered_at { get; set; }
        public DateTime? delivered_at { get; set; }
        /// <summary>HTTPS object-storage URL for a violation snapshot JPEG/PNG, if captured.</summary>
        public string? snapshot_url { get; set; }
        public MonitoringEvent MonitoringEvent { get; set; }
        public StudentSession StudentSession { get; set; }
        public ICollection<ProctorAction> ProctorActions { get; set; } = new List<ProctorAction>();
    }
} 
