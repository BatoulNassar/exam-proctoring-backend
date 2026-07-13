using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class ConnectivityBuffer : BaseEntity
    {
        public int student_session_id { get; set; }
        public string buffer_type { get; set; }
        public string encrypted_payload { get; set; }
        public string action  { get; set; }
        public DateTime buffered_at { get; set; }
        public DateTime?synced_at { get; set; }
        
        public StudentSession StudentSession { get; set; }
    }
}
