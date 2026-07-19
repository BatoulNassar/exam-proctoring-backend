using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

public class SuperAdminSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public SuperAdminSeeder(AppDbContext context)
    {
        _context = context;
    }

    public SuperAdminSeeder(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync()
    {
        if (await _context.Users.AnyAsync(u => u.email == "admin@exam.com")) return;

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.name == "SuperAdmin");

        if (role == null)
        {
            role = new Role { name = "SuperAdmin", created_at = DateTime.UtcNow };
            await _context.Roles.AddAsync(role);
            await _context.SaveChangesAsync();
        }

        string plainPassword = "Admin123!";
        var admin = new User
        {
            user_name = "superadmin",
            email = "admin@exam.com",
            full_name = "System Administrator",
            password_hash = _passwordHasher.Hash(plainPassword),
            created_at = DateTime.UtcNow
        };

        await _context.Users.AddAsync(admin);
        await _context.SaveChangesAsync();

        var userRole = new User_Roles
        {
            user_id = admin.id,
            role_id = role.id,
            created_at = DateTime.UtcNow
        };

        await _context.UserRoles.AddAsync(userRole);
        await _context.SaveChangesAsync();
    }
}