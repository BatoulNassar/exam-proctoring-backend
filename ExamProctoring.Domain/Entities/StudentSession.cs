using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
     public class StudentSession : BaseEntity
    {
        public int exam_session_id { get; set; }
        public int student_id { get; set; }
        public StudentSessionStatus status { get; set; }
        public DateTime? login_at { get; set; }
        public DateTime? verified_at { get; set; }
        public bool liveness_passed { get; set; }
        public bool face_match_passed { get; set; }
        public int failed_auth_attempts { get; set; }
        public DateTime? submitted_at { get; set; }  
        public int? awarded_marks { get; set; }

        public Student Student { get; set; }
        public ICollection<AutoScore> AutoScores { set; get; } = new List<AutoScore>();
        public ICollection <StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
        public ICollection <AlertEvent> Alerts { set; get; }= new List <AlertEvent>();
        public ICollection<ConnectivityBuffer> ConnectivityBuffers { get; set; } = new List<ConnectivityBuffer>();
        public ICollection<MonitoringEvent> MonitoringEvents { get; set; } = new List<MonitoringEvent>();
        public ICollection<WarningMessage> WarningMessages { get; set; } = new List<WarningMessage>();
        public ExamSession ExamSession { get; set; }
    }
}
