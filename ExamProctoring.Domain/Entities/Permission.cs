using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class Permission : BaseEntity
    {
        public string name { get; set; }
        public string description { get; set; }

        public ICollection<PermissionRole> Permission_Roles { get; set; } = new List<PermissionRole>();
    }  
}
