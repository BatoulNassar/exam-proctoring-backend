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
        public ICollection<User_Roles> UserRoles { get; set; } = new List<User_Roles>();
        public ICollection<PermissionRole> PermissionRoles { get; set; } = new List<PermissionRole>();
    }
}
