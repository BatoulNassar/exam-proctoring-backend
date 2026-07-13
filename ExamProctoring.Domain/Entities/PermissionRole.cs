using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class PermissionRole : BaseEntity
    {
        public int permission_id { get; set; }
        public int role_id { get; set; }

        public Role Role { get; set; }
        public Permission Permission { get; set; }
    }
}
