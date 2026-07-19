using ExamProctoring.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Domain.Entities
{
    public class User : BaseEntity 
    {
        public string? user_name { get; set; }
        public string email { get; set; }
        public string? phone_number { get; set; }
        public string full_name { get; set; }
        public string password_hash { get; set; }

        public ICollection<ProctorAction> ProctorActions { get; set; }= new List<ProctorAction>();
        public ICollection<User_Roles> UserRoles { get; set; } =new List<User_Roles>();
        public ICollection<ExamSession> ExamSessions { get; set; } = new List <ExamSession>();
        public ICollection<QuestionBank> QuestionBanks { get; set; } = new List<QuestionBank>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
