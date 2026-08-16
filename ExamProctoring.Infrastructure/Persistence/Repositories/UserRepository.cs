using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetByEmailWithRolesAndPermissionsAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.PermissionRoles)
                            .ThenInclude(pr => pr.Permission)
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.email == email);
        }

        public async Task<User> GetByIdWithRolesAndPermissionsAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.PermissionRoles)
                            .ThenInclude(pr => pr.Permission)
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.id == userId);
        }

        public async Task<User?> GetByIdWithRolesAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .SingleOrDefaultAsync(u => u.id == userId);
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(string roleName)
        {
            return await _context.Users
                .Where(u => u.UserRoles.Any(ur => ur.Role.name == roleName))
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<User> GetByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.id == userId);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetAdminsWithPermissionsPagedAsync(int page, int pageSize)
        {
            return await _context.Users
                .Where(u => u.UserRoles.Any(ur => ur.Role.name == "Admin" || ur.Role.name == "Proctor"))
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.PermissionRoles)
                            .ThenInclude(pr => pr.Permission)
                .OrderBy(u => u.full_name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAdminsWithPermissionsAsync()
        {
            return await _context.Users
                .CountAsync(u => u.UserRoles.Any(ur => ur.Role.name == "Admin" || ur.Role.name == "Proctor"));
        }

        public async Task<IEnumerable<User>> GetAdminsBasicPagedAsync(int page, int pageSize)
        {
            return await _context.Users
                .Where(u => u.UserRoles.Any(ur => ur.Role.name == "Admin"))
                .OrderBy(u => u.full_name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAdminsAsync()
        {
            return await _context.Users
                .CountAsync(u => u.UserRoles.Any(ur => ur.Role.name == "Admin"));
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.email == email);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.email == email);
        }

        public async Task DeleteAsync(User user)
        {
            user.is_deleted = true;
            user.deleted_at = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<(IReadOnlyList<dynamic> Proctors, int TotalCount)> GetAllProctorsPagedAsync(int page, int pageSize)
        {
            var totalCount = await _context.Users
                .CountAsync(u => u.UserRoles.Any(ur => ur.Role.name == "Proctor"));

            var proctors = await _context.Users
                .Where(u => u.UserRoles.Any(ur => ur.Role.name == "Proctor"))
                .OrderBy(u => u.full_name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    ProctorId = u.id,
                    u.full_name,
                    u.email,
                    u.phone_number,
                    u.is_active,
                    AssignedSessionsCount = u.ProctorSessions.Count(),
                    ActiveSessionsCount = u.ProctorSessions.Count(ps => ps.ExamSession.status == Domain.Enums.ExamSessionStatus.ACTIVE || ps.ExamSession.status == Domain.Enums.ExamSessionStatus.GRACE)
                })
                .ToListAsync();

            return (proctors.Cast<dynamic>().ToList(), totalCount);
        }
    }
}