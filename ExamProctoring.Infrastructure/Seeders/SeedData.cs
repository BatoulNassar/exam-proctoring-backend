using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Infrastructure.Data;
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

            if (!includeDemoData)
                return;

            // Demo/test data only: proctors, students, exams, monitoring and audit samples.
            var demoDataSeeder = new DemoDataSeeder(context, passwordHasher);
            await demoDataSeeder.SeedAsync();
        }
    }
}
