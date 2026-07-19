using ExamProctoring.Application.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class PermissionRoleRepository : IPermissionRoleRepository
{
    private readonly AppDbContext _context;

    public PermissionRoleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PermissionRole permissionRole)
    {
        await _context.PermissionRoles.AddAsync(permissionRole);
    }

    public async Task RemoveByRoleIdAsync(int roleId)
    {
        var permissions = await _context.PermissionRoles.Where(pr => pr.role_id == roleId).ToListAsync();

        _context.PermissionRoles.RemoveRange(permissions);
    }

    public async Task RemoveAsync(int roleId, int permissionId)
    {
        var item = await _context.PermissionRoles.FirstOrDefaultAsync(pr => pr.role_id == roleId && pr.permission_id == permissionId);

        if (item != null)
        {
            _context.PermissionRoles.Remove(item);
        }
    }
}