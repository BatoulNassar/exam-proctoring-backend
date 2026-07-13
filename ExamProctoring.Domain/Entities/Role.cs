using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class Role : BaseEntity
    {
        public string name { get; set; }
        public ICollection<User_Roles> User_Roles { get; set; } = new List<User_Roles>();
        public ICollection<PermissionRole> Permission_Roles { get; set; } = new List<PermissionRole>();
    }
}
