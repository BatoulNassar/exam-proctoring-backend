using ExamProctoring.Application.Features.QuestionBank.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamProctoring.API.Services
{
    public class QuestionBankStateCheckBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QuestionBankStateCheckBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

        public QuestionBankStateCheckBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<QuestionBankStateCheckBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Question bank state check background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var transitionService = scope.ServiceProvider
                        .GetRequiredService<IQuestionBankStateTransitionService>();

                    await transitionService.CheckAndUpdateBankStatesAsync();
                    _logger.LogInformation("Question bank states checked at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking question bank states");
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

            _logger.LogInformation("Question bank state check background service stopped");
        }
    }
}
