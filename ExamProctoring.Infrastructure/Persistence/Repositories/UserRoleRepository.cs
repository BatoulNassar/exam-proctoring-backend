using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly AppDbContext _context;

        public UserRoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(User_Roles userRole)
        {
            await _context.UserRoles.AddAsync(userRole);
        }

        public async Task RemoveByUserIdAsync(int userId)
        {
            var userRoles = await _context.UserRoles
                 .Where(ur => ur.user_id == userId)
                 .ToListAsync();

            foreach (var role in userRoles)
            {
                _context.UserRoles.Remove(role);
            }
        }
    }
}
