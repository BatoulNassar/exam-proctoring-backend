using ExamProctoring.Domain.Entities;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IUserRoleRepository
    {
        Task AddAsync(User_Roles userRole);
        Task RemoveByUserIdAsync(int userId);
    }
}
