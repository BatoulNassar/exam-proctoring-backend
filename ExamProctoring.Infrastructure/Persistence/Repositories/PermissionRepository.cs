using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly AppDbContext _context;

        public PermissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Permission> GetByNameAsync(string permissionName)
        {
            return await _context.Permissions.FirstOrDefaultAsync(p => p.name == permissionName);
        }

        public async Task<IList<Permission>> GetByNamesAsync(IEnumerable<string> permissionNames)
        {
            return await _context.Permissions
            .Where(p => permissionNames.Contains(p.name))
            .ToListAsync();
        }
    }
}
