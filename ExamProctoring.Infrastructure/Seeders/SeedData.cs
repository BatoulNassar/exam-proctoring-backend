using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Seeders
{
    public class SeedData
    {
        /// Seeds the data the system needs to function (roles, permissions, super admin) and, when
        /// <paramref name="includeDemoData"/> is set, the development demo data as well.
        /// The caller owns the service scope, so no additional scope is created here.
        public static async Task InitializeAsync(IServiceProvider scopedServiceProvider, bool includeDemoData)
        {
            var context = scopedServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scopedServiceProvider.GetRequiredService<IPasswordHasher>();

            // Required bootstrap data: roles and permissions first, then the super admin.
            var rolePermissionSeeder = new RoleAndPermissionSeeder(context);
            await rolePermissionSeeder.SeedAsync();

            var superAdminSeeder = new SuperAdminSeeder(context, passwordHasher);
            await superAdminSeeder.SeedAsync();

            // Operational configuration, not demo data: without this row every
            // settings read returns null and PUT /api/settings answers "not found".
            // It must be seeded before the demo-data gate below, or a server that
            // only runs the bootstrap seed ends up with an empty settings table.
            await SeedDefaultSystemSettingsAsync(context);

            if (!includeDemoData)
                return;

            // Demo/test data only: proctors, students, exams, monitoring and audit samples.
            var demoDataSeeder = new DemoDataSeeder(context, passwordHasher);
            await demoDataSeeder.SeedAsync();
        }

        /// Writes the single settings row if the table is empty. Existing values are
        /// never overwritten, so an administrator's changes survive a restart.
        private static async Task SeedDefaultSystemSettingsAsync(AppDbContext context)
        {
            if (await context.SystemSettings.AnyAsync())
                return;

            context.SystemSettings.Add(new ExamProctoring.Domain.Entities.SystemSettings
            {
                gaze_alert_threshold_sec  = 3,
                face_sensitivity          = ExamProctoring.Domain.Enums.FaceSensitivity.Medium,
                ambient_audio_monitoring  = true,
                grace_period_minutes      = 5,
                login_window_minutes      = 15,
                max_liveness_attempts     = 3,
                face_match_threshold      = 95,
                question_randomisation    = true,
                option_shuffle            = false,
                max_warnings_before_termination = 3,
                created_at                = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }
}
