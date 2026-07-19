using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByEmailWithRolesAndPermissionsAsync(string email);
        Task<User> GetByIdWithRolesAndPermissionsAsync(int userId);
        Task AddAsync(User user);
        Task<IEnumerable<User>> GetAdminsWithPermissionsPagedAsync(int page, int pageSize);
        Task<bool> EmailExistsAsync(string email);
        Task DeleteAsync(User user);
    }
}