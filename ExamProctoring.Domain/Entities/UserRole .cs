using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class User_Roles : BaseEntity
    {
      public int user_id { get; set; }
      public int role_id { get; set; }

       public User User { get; set; }
       public Role Role { get; set; }
    }
}
