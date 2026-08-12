using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamProctoring.Infrastructure.Seeders
{
    /// <summary>
    /// Keeps roles and permissions in step with the endpoints the API actually
    /// exposes. The permission catalogue below is derived from the controllers:
    /// every entry maps to at least one real endpoint, and the per-role lists
    /// mirror the [Authorize] attributes those endpoints carry today.
    /// </summary>
    public class RoleAndPermissionSeeder
    {
        private readonly AppDbContext _context;

        public RoleAndPermissionSeeder(AppDbContext context)
        {
            _context = context;
        }

        // ===== Exam sessions — ExamSessionsController =====
        private const string CreateExamSession = "CreateExamSession";
        private const string ViewExamSession = "ViewExamSession";
        private const string EditExamSession = "EditExamSession";
        private const string DeleteExamSession = "DeleteExamSession";
        private const string RestoreExamSession = "RestoreExamSession";
        private const string PublishExamSession = "PublishExamSession";
        private const string ExtendSessionTime = "ExtendSessionTime";

        // ===== Live monitoring — MonitoringController =====
        private const string MonitorExamSession = "MonitorExamSession";

        // ===== Students — StudentsController =====
        private const string ViewStudents = "ViewStudents";
        private const string ImportStudents = "ImportStudents";

        // ===== Proctors — ExamSessionsController (assignment + availability) =====
        private const string ViewProctors = "ViewProctors";
        private const string AssignProctor = "AssignProctor";

        // ===== Admin users — UserController =====
        private const string CreateUser = "CreateUser";
        private const string ViewUsers = "ViewUsers";
        private const string EditUser = "EditUser";
        private const string DeleteUser = "DeleteUser";

        // ===== Roles and permissions — RoleController =====
        private const string ManageRoles = "ManageRoles";
        private const string ManagePermissions = "ManagePermissions";

        // ===== System settings — SystemSettingsController =====
        private const string ViewSystemSettings = "ViewSystemSettings";
        private const string ManageSystemSettings = "ManageSystemSettings";

        // ===== Audit and reports — AuditLogsController, session exports =====
        private const string ViewAuditLogs = "ViewAuditLogs";
        private const string ViewReports = "ViewReports";
        private const string ExportData = "ExportData";

        // ===== Alerts — AlertsController =====
        // One permission per action: the three differ sharply in consequence, so
        // they must be grantable separately.
        private const string ViewAlerts = "ViewAlerts";
        private const string DismissAlert = "DismissAlert";
        private const string WarnStudent = "WarnStudent";
        private const string EscalateAlert = "EscalateAlert";

        // ===== Dashboards — DashboardController, ProctorDashboardController =====
        private const string ViewDashboard = "ViewDashboard";
        private const string ViewProctorDashboard = "ViewProctorDashboard";

        // ===== Question banks — QuestionBankController =====
        private const string ViewQuestionBanks = "ViewQuestionBanks";
        private const string ManageQuestionBanks = "ManageQuestionBanks";

        private static readonly (string Name, string Description)[] Catalogue =
        {
            (CreateExamSession,    "Create exam sessions"),
            (ViewExamSession,      "View exam sessions and their details"),
            (EditExamSession,      "Edit an exam session"),
            (DeleteExamSession,    "Soft-delete an exam session"),
            (RestoreExamSession,   "Restore a soft-deleted exam session"),
            (PublishExamSession,   "Publish an exam session"),
            (ExtendSessionTime,    "Extend the running time of a session"),

            (MonitorExamSession,   "Watch active sessions and their students live"),

            (ViewStudents,         "View the student registry"),
            (ImportStudents,       "Import students from a CSV roster"),

            (ViewProctors,         "View proctors and their availability"),
            (AssignProctor,        "Assign proctors to an exam session"),

            (CreateUser,           "Create admin users"),
            (ViewUsers,            "View admin users and their permissions"),
            (EditUser,             "Deactivate or reactivate an admin user"),
            (DeleteUser,           "Delete an admin user"),

            (ManageRoles,          "View and manage roles"),
            (ManagePermissions,    "Change the permissions granted to a role"),

            (ViewSystemSettings,   "View global proctoring settings"),
            (ManageSystemSettings, "Change global proctoring settings"),

            (ViewAuditLogs,        "View the audit log"),
            (ViewReports,          "View reports"),
            (ExportData,           "Export grading reports and audit logs"),

            (ViewAlerts,           "View proctoring alerts"),
            (DismissAlert,         "Dismiss an alert as a false positive"),
            (WarnStudent,          "Send a warning to a student during an exam"),
            (EscalateAlert,        "Escalate an alert for disciplinary review"),

            (ViewDashboard,        "View the admin dashboard"),
            (ViewProctorDashboard, "View the proctor dashboard"),

            (ViewQuestionBanks,    "View question banks and their questions"),
            (ManageQuestionBanks,  "Create, upload and delete question banks"),
        };

        private static readonly string[] SuperAdminPermissions =
            Catalogue.Select(p => p.Name).ToArray();

        /// <summary>
        /// Everything an Admin can reach today. Excludes user, role, permission and
        /// system-settings management, and session restore: those endpoints are
        /// marked SuperAdmin only.
        /// </summary>
        private static readonly string[] AdminPermissions =
        {
            CreateExamSession, ViewExamSession, EditExamSession, DeleteExamSession,
            PublishExamSession, ExtendSessionTime,
            MonitorExamSession,
            ViewStudents, ImportStudents,
            ViewProctors, AssignProctor,
            ViewAuditLogs, ViewReports, ExportData,
            ViewAlerts, DismissAlert, WarnStudent, EscalateAlert,
            ViewDashboard,
            ViewQuestionBanks, ManageQuestionBanks,
        };

        /// <summary>
        /// Everything a Proctor can reach today. Note the alert actions: only
        /// WarnStudent is currently allowed by AlertsController.
        /// </summary>
        private static readonly string[] ProctorPermissions =
        {
            ExtendSessionTime,
            MonitorExamSession,
            ViewStudents,
            ViewAlerts, WarnStudent,
            ViewProctorDashboard,
        };

        public async Task SeedAsync()
        {
            // The catalogue is owned by the code and fully reconciled. Role grants are
            // owned by the data once seeded: the roles screen can change them, so only
            // permissions that are brand new to this database get granted below.
            var newlyCreated = await SeedPermissionsAsync();
            await SeedRolesAsync();
            await SeedRolePermissionsAsync(newlyCreated);
            await RemoveRetiredPermissionsAsync();
        }

        /// <returns>Names of the permissions this run created, empty if none.</returns>
        private async Task<HashSet<string>> SeedPermissionsAsync()
        {
            var existing = await _context.Permissions.ToListAsync();
            var existingByName = existing.ToDictionary(p => p.name);

            // Descriptions are owned by the code, so they are kept in step rather than
            // written once. Databases seeded by an earlier version carry placeholder
            // text like "Permission to CreateExamSession", which is useless on the
            // roles screen. Grants are data and are never touched here.
            var changed = false;
            foreach (var entry in Catalogue)
            {
                if (existingByName.TryGetValue(entry.Name, out var permission)
                    && permission.description != entry.Description)
                {
                    permission.description = entry.Description;
                    permission.updated_at = DateTime.UtcNow;
                    changed = true;
                }
            }

            var missing = Catalogue
                .Where(p => !existingByName.ContainsKey(p.Name))
                .Select(p => new Permission
                {
                    name = p.Name,
                    description = p.Description,
                    created_at = DateTime.UtcNow
                })
                .ToList();

            if (missing.Count > 0)
                await _context.Permissions.AddRangeAsync(missing);

            if (changed || missing.Count > 0)
                await _context.SaveChangesAsync();

            return missing.Select(p => p.name).ToHashSet();
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

        private async Task SeedRolePermissionsAsync(HashSet<string> newlyCreated)
        {
            var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.name == "SuperAdmin");
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.name == "Admin");
            var proctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.name == "Proctor");

            if (superAdminRole == null || adminRole == null || proctorRole == null)
                return;

            await AssignPermissionsToRoleAsync(superAdminRole, SuperAdminPermissions, newlyCreated);
            await AssignPermissionsToRoleAsync(adminRole, AdminPermissions, newlyCreated);
            await AssignPermissionsToRoleAsync(proctorRole, ProctorPermissions, newlyCreated);
        }

        /// <summary>
        /// Grants the role its defaults, but only for permissions that did not exist
        /// before this run — or for a role that has no grants at all, which means a
        /// fresh database or a newly added role.
        /// </summary>
        /// <remarks>
        /// Deliberately never re-grants an existing permission. The roles screen lets a
        /// SuperAdmin revoke one; re-adding it on the next restart would silently undo
        /// their decision, and the UI would look like it had not saved.
        /// </remarks>
        private async Task AssignPermissionsToRoleAsync(Role role, string[] permissionNames, HashSet<string> newlyCreated)
        {
            var existingPermissions = await _context.PermissionRoles
                .Where(pr => pr.role_id == role.id)
                .Select(pr => pr.Permission.name)
                .ToListAsync();

            var candidates = existingPermissions.Count == 0
                ? permissionNames.AsEnumerable()          // fresh role: seed the full default set
                : permissionNames.Where(newlyCreated.Contains); // established role: only brand-new permissions

            var toGrant = candidates.Except(existingPermissions).ToList();
            if (toGrant.Count == 0) return;

            var permissions = await _context.Permissions
                .Where(p => toGrant.Contains(p.name))
                .ToListAsync();

            var links = permissions.Select(p => new PermissionRole
            {
                role_id = role.id,
                permission_id = p.id,
                created_at = DateTime.UtcNow
            });

            await _context.PermissionRoles.AddRangeAsync(links);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes permissions that are no longer in the catalogue, together with
        /// their role grants. Without this, permissions dropped from the code keep
        /// showing up on the roles screen and inside issued tokens.
        /// </summary>
        private async Task RemoveRetiredPermissionsAsync()
        {
            var current = Catalogue.Select(p => p.Name).ToList();

            var retired = await _context.Permissions
                .Where(p => !current.Contains(p.name))
                .ToListAsync();

            if (retired.Count == 0) return;

            var retiredIds = retired.Select(p => p.id).ToList();

            var links = await _context.PermissionRoles
                .Where(pr => retiredIds.Contains(pr.permission_id))
                .ToListAsync();

            _context.PermissionRoles.RemoveRange(links);
            _context.Permissions.RemoveRange(retired);
            await _context.SaveChangesAsync();
        }
    }
}
