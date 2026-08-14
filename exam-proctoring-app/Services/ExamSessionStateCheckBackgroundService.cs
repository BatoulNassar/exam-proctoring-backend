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
        // A session's start time is a promise to students and proctors, so the gap
        // between that time and the status catching up has to stay small. At five
        // minutes an exam due at 10:00 could still read LOCKED at 10:04.
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

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
