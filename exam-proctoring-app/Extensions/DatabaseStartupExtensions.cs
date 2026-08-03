using ExamProctoring.Infrastructure.Data;
using ExamProctoring.Infrastructure.Seeders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ExamProctoring.API.Extensions
{
    public static class DatabaseStartupExtensions
    {
        /// Configuration flag that lets an operator deliberately run the required bootstrap seeders
        /// (roles, permissions, super admin) once outside Development. Demo data is never included.
        private const string RunBootstrapSeedKey = "Database:RunBootstrapSeedOnStartup";

        /// Reports which server, database and login the deployed instance will actually use, so a wrong
        /// configuration source can be spotted from the log. The password is never read or written.
        private static void LogEffectiveConnectionTarget(WebApplication app, ILogger logger)
        {
            const string connectionKey = "ServerConnection";
            var connectionString = app.Configuration.GetConnectionString(connectionKey);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogError(
                    "Environment {Environment}: connection string '{ConnectionKey}' is missing or empty.",
                    app.Environment.EnvironmentName, connectionKey);
                return;
            }

            try
            {
                var csb = new SqlConnectionStringBuilder(connectionString);

                logger.LogInformation(
                    "Environment {Environment}: using connection key '{ConnectionKey}' -> server {DataSource}, database {Database}, login {UserId}, integrated security {IntegratedSecurity}, connect timeout {ConnectTimeout}s.",
                    app.Environment.EnvironmentName,
                    connectionKey,
                    csb.DataSource,
                    csb.InitialCatalog,
                    string.IsNullOrEmpty(csb.UserID) ? "(none)" : csb.UserID,
                    csb.IntegratedSecurity,
                    csb.ConnectTimeout);
            }
            catch (ArgumentException ex)
            {
                logger.LogError(
                    "Environment {Environment}: connection string '{ConnectionKey}' could not be parsed: {Reason}",
                    app.Environment.EnvironmentName, connectionKey, ex.Message);
            }
        }

        /// Development applies migrations and seeds development data.
        /// Outside Development the application performs no schema change and no database access at all
        /// during startup, so a database that is unreachable or has a mismatched migration history can
        /// never take the API down with an opaque startup failure.
        public static async Task InitializeDatabaseAsync(this WebApplication app)
        {
            var logger = app.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Startup.Database");

            LogEffectiveConnectionTarget(app, logger);

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
