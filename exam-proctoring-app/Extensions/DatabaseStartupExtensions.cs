using ExamProctoring.Infrastructure.Data;
using ExamProctoring.Infrastructure.Seeders;
using Microsoft.EntityFrameworkCore;

namespace ExamProctoring.API.Extensions
{
    public static class DatabaseStartupExtensions
    {
        /// Configuration flag that lets an operator deliberately run the required bootstrap seeders
        /// (roles, permissions, super admin) once outside Development. Demo data is never included.
        private const string RunBootstrapSeedKey = "Database:RunBootstrapSeedOnStartup";

        /// Development applies migrations and seeds development data.
        /// Outside Development the application performs no schema change and no database access at all
        /// during startup, so a database that is unreachable or has a mismatched migration history can
        /// never take the API down with an opaque startup failure.
        public static async Task InitializeDatabaseAsync(this WebApplication app)
        {
            var logger = app.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Startup.Database");

            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            if (app.Environment.IsDevelopment())
            {
                var dbContext = services.GetRequiredService<AppDbContext>();

                var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
                logger.LogInformation(
                    "Development startup: {PendingCount} pending migration(s) to apply: {Migrations}",
                    pending.Count,
                    pending.Count == 0 ? "(none)" : string.Join(", ", pending));

                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Development startup: migrations applied.");

                await SeedData.InitializeAsync(services, includeDemoData: true);
                logger.LogInformation("Development startup: bootstrap and demo data seeded.");

                return;
            }

            logger.LogInformation(
                "{Environment} startup: automatic migrations and demo seeding are disabled. " +
                "Apply schema changes deliberately with a generated SQL script.",
                app.Environment.EnvironmentName);

            if (!app.Configuration.GetValue<bool>(RunBootstrapSeedKey))
                return;

            logger.LogWarning(
                "{Environment} startup: bootstrap seeding is enabled via {ConfigKey}. " +
                "Roles, permissions and the super admin will be created if missing; no demo data is written.",
                app.Environment.EnvironmentName, RunBootstrapSeedKey);

            await SeedData.InitializeAsync(services, includeDemoData: false);

            logger.LogInformation("{Environment} startup: bootstrap seeding completed.", app.Environment.EnvironmentName);
        }
    }
}
