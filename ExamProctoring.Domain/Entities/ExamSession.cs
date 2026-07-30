using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class ExamSession : BaseEntity
    {
        public string title { get; set; }
        public string course_tag { get; set; }
        public ExamSessionStatus status { get; set; }
        public DateTime start_time { get; set; }
        public int duration_minutes { get; set; }
        public int question_bank_id { get; set; }
        public DateTime? scheduled_at { get; set; }
        public DateTime? locked_at { get; set; }
        public DateTime? active_at { get; set; }
        public DateTime? grace_period_ended_at { get; set; }
        public int grace_period_minutes { get; set; }
        public int extended_by_minutes { get; set; }
        public int login_window_minutes { get; set; }
        public int eye_gaze_threshold_sec { get; set; }
        public FaceAlertSensitivity face_alert_sensitivity { get; set; } = FaceAlertSensitivity.Medium;
        public int created_by_admin_id { get; set; }
        public DateTime? closed_at { get; set; }
        public DateTime? archived_at { get; set; }

        public User CreatedByAdmin { get; set; }
        public ICollection<AuditLog> AuditLogs { set; get; } = new List <AuditLog>();
        public QuestionBank QuestionBank { get; set; }
        public ICollection<StudentSession> StudentSessions { set; get; } = new List<StudentSession>();
        public ICollection <GradingExport> GradingExports { set; get; } = new List <GradingExport>();
        public ICollection<ProctorSession> ProctorSessions { set; get; } = new List<ProctorSession>();
    }
}
