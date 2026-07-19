using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Role> GetByIdAsync(int id)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.id == id);
        }

        public async Task<Role> GetByIdWithPermissionsAsync(int roleId)
        {
            return await _context.Roles
                .Include(r => r.PermissionRoles)
                .ThenInclude(pr => pr.Permission)
                .FirstOrDefaultAsync(r => r.id == roleId);
        }

        public async Task<IEnumerable<Role>> GetAllWithPermissionsAsync()
        {
            return await _context.Roles
                .Include(r => r.PermissionRoles)
                .ThenInclude(pr => pr.Permission)
                .OrderBy(r => r.name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Role>> GetByIdsAsync(List<int> roleIds)
        {
            return await _context.Roles.Where(r => roleIds.Contains(r.id)).ToListAsync();
        }

        public async Task<Role> GetByNameAsync(string roleName)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.name == roleName);
        }
    }
}