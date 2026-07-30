using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamProctoring.Infrastructure.Seeders
{
    public class RoleAndPermissionSeeder
    {
        private readonly AppDbContext _context;

        public RoleAndPermissionSeeder(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            await SeedPermissionsAsync();
            await SeedRolesAsync();
            await SeedRolePermissionsAsync();
        }

        private async Task SeedPermissionsAsync()
        {
            var requiredPermissions = new[]
            {
                // Exam Session Permissions
                "CreateExamSession",
                "ViewExamSession",
                "EditExamSession",
                "DeleteExamSession",
                "PublishExamSession",
                "MonitorExamSession",
                "ExtendSessionTime",

                // Student Management
                "ViewStudents",
                "EnrollStudents",
                "RemoveStudents",

                // Proctor Management
                "AssignProctor",
                "ViewProctors",
                "ManageProctorActions",

                // User Management
                "CreateUser",
                "ViewUsers",
                "EditUser",
                "DeleteUser",

                // Role & Permission Management
                "ManageRoles",
                "ManagePermissions",

                // Audit & Reports
                "ViewAuditLogs",
                "ViewReports",
                "ExportData",

                // Alert Management
                "ViewAlerts",
                "TakeProctorAction",

                // Question Bank
                "ManageQuestionBanks",
                "ViewQuestionBanks",
            };

            foreach (var permissionName in requiredPermissions)
            {
                if (!await _context.Permissions.AnyAsync(p => p.name == permissionName))
                {
                    await _context.Permissions.AddAsync(new Permission
                    {
                        name = permissionName,
                        description = $"Permission to {permissionName}",
                        created_at = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedRolesAsync()
        {
            var requiredRoles = new[] { "SuperAdmin", "Admin", "Proctor" };

            foreach (var roleName in requiredRoles)
            {
                if (!await _context.Roles.AnyAsync(r => r.name == roleName))
                {
                    await _context.Roles.AddAsync(new Role
                    {
                        name = roleName,
                        created_at = DateTime.UtcNow
                    });
                }
            } 

            await _context.SaveChangesAsync();
        }

        private async Task SeedRolePermissionsAsync()
        {
            var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.name == "SuperAdmin");
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.name == "Admin");
            var proctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.name == "Proctor");
            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.name == "Student");

            if (superAdminRole == null || adminRole == null || proctorRole == null || studentRole == null)
                return;

            // SuperAdmin gets all permissions
            await AssignPermissionsToRoleAsync(superAdminRole, new[]
            {
                "CreateExamSession", "ViewExamSession", "EditExamSession", "DeleteExamSession", "PublishExamSession",
                "MonitorExamSession", "ExtendSessionTime", "ViewStudents", "EnrollStudents", "RemoveStudents",
                "AssignProctor", "ViewProctors", "ManageProctorActions", "CreateUser", "ViewUsers", "EditUser",
                "DeleteUser", "ManageRoles", "ManagePermissions", "ViewAuditLogs", "ViewReports", "ExportData",
                "ViewAlerts", "TakeProctorAction", "ManageQuestionBanks", "ViewQuestionBanks"
            });

            // Admin permissions
            await AssignPermissionsToRoleAsync(adminRole, new[]
            {
                "CreateExamSession", "ViewExamSession", "EditExamSession", "DeleteExamSession", "PublishExamSession",
                "MonitorExamSession", "ViewStudents", "EnrollStudents", "RemoveStudents", "AssignProctor",
                "ViewProctors", "ManageProctorActions", "ViewUsers", "ViewAuditLogs", "ViewReports",
                "ViewAlerts", "TakeProctorAction", "ManageQuestionBanks", "ViewQuestionBanks"
            });

            // Proctor permissions
            await AssignPermissionsToRoleAsync(proctorRole, new[]
            {
                "ViewExamSession", "MonitorExamSession", "ViewStudents", "ViewAlerts", "TakeProctorAction"
            });

            // Student permissions (minimal)
            await AssignPermissionsToRoleAsync(studentRole, new[]
            {
                "ViewExamSession"
            });
        }

        private async Task AssignPermissionsToRoleAsync(Role role, string[] permissionNames)
        {
            var existingPermissions = await _context.PermissionRoles
                .Where(pr => pr.role_id == role.id)
                .Select(pr => pr.Permission.name)
                .ToListAsync();

            foreach (var permissionName in permissionNames)
            {
                if (!existingPermissions.Contains(permissionName))
                {
                    var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.name == permissionName);
                    if (permission != null)
                    {
                        await _context.PermissionRoles.AddAsync(new PermissionRole
                        {
                            role_id = role.id,
                            permission_id = permission.id,
                            created_at = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
