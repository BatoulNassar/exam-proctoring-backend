using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByEmailWithRolesAndPermissionsAsync(string email);
        Task<User> GetByIdWithRolesAndPermissionsAsync(int userId);
        Task<User?> GetByIdWithRolesAsync(int userId);
        Task<User> GetByIdAsync(int userId);
        Task<IEnumerable<User>> GetByRoleAsync(string roleName);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task<IEnumerable<User>> GetAdminsWithPermissionsPagedAsync(int page, int pageSize);

        /// <summary>
        /// Total for <see cref="GetAdminsWithPermissionsPagedAsync"/>. Must stay in
        /// step with that query's filter, which covers Admin and Proctor alike —
        /// CountAdminsAsync counts Admin only and would under-report here.
        /// </summary>
        Task<int> CountAdminsWithPermissionsAsync();
        Task<int> CountAdminsAsync();
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetByEmailAsync(string email);
        Task DeleteAsync(User user);

        /// <summary>
        /// Paginated list of all proctors with their session and alert counts.
        /// </summary>
        Task<(IReadOnlyList<dynamic> Proctors, int TotalCount)> GetAllProctorsPagedAsync(int page, int pageSize);
    }
}