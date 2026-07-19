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

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task<IEnumerable<User>> GetAdminsWithPermissionsPagedAsync(int page, int pageSize)
        {
            return await _context.Users
                .Where(u => u.UserRoles.Any(ur => ur.Role.name == "Admin" ))
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.full_name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
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

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.email == email);
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}