using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public int user_id { get; set; }
        public string token { get; set; }
        public DateTime expires_at { get; set; }
        public DateTime? revoked_at { get; set; }
        public string? replaced_by_token { get; set; }

        public User User { get; set; }
    }
}
