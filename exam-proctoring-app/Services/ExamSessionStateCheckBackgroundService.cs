using ExamProctoring.Application.Features.ExamAttempts.Services;
using ExamProctoring.Application.Features.ExamSessions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamProctoring.API.Services
{
    public class ExamSessionStateCheckBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExamSessionStateCheckBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

        public ExamSessionStateCheckBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ExamSessionStateCheckBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Exam session state check background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var transitionService = scope.ServiceProvider.GetRequiredService<IExamSessionStateTransitionService>();

                    await transitionService.CheckAndUpdateSessionStatesAsync();
                    _logger.LogInformation("Exam session states checked and updated at {time}", DateTime.UtcNow);

                    // Janitor for attempts nobody submitted. Deliberately runs on the same tick as
                    // the session transitions rather than in its own hosted service, and is NOT the
                    // deadline authority: PUT Answer checks ends_at on every request, so this
                    // interval can never let a late answer through.
                    var expiryService = scope.ServiceProvider.GetRequiredService<IAttemptExpiryService>();
                    var finalised = await expiryService.FinaliseExpiredAttemptsAsync();

                    if (finalised > 0)
                        _logger.LogInformation("Auto-finalised {Count} expired attempt(s).", finalised);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking and updating exam session states");
                }

                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Exam session state check background service stopped");
        }
    }
}
