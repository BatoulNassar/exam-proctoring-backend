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
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>(); // استخراج الخدمة

            var superAdminSeeder = new SuperAdminSeeder(context, passwordHasher);
            await superAdminSeeder.SeedAsync();
        }
    }
}
