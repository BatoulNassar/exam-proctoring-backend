using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class Student : BaseEntity
    {
        public string user_name { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string phone_number { get; set; }
        public string first_name { get; set; }
        public string middle_name { get; set; }
        public string last_name { get; set; }
        public string university_number { get; set; }
        public string face_id { get; set; }
        public string photo_url { get; set; }

        public bool is_active { get; set; } = true;
        public int failed_login_attempts { get; set; }
        public DateTime? lockout_end_utc { get; set; }

        public ICollection<StudentSession> StudentSessions { get; set; } = new List<StudentSession>();
    }
}
