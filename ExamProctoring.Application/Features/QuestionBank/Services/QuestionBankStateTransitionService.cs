using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ExamProctoring.Application.Features.QuestionBank.Services
{
    public interface IQuestionBankStateTransitionService
    {
        Task CheckAndUpdateBankStatesAsync();
    }

    public class QuestionBankStateTransitionService : IQuestionBankStateTransitionService
    {
        private readonly IQuestionBankRepository _repository;
        private readonly ILogger<QuestionBankStateTransitionService> _logger;

        public QuestionBankStateTransitionService(
            IQuestionBankRepository repository,
            ILogger<QuestionBankStateTransitionService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task CheckAndUpdateBankStatesAsync()
        {
            var now = DateTime.UtcNow;

            await LockDraftBanksAsync(now);
            await ArchiveLockedBanksAsync(now);
        }

        private async Task LockDraftBanksAsync(DateTime now)
        {
            var lockCutoff = now.AddHours(24);

            var draftBanks = await _repository.GetDraftBanksWithSessionsAsync();

            var toLock = draftBanks
                .Where(qb => qb.ExamSessions.Any(es => es.start_time <= lockCutoff))
                .ToList();

            foreach (var bank in toLock)
            {
                bank.status = QuestionBankStatus.Locked;
                bank.locked_at = now;
                bank.updated_at = now;
                await _repository.UpdateAsync(bank);
                _logger.LogInformation("QuestionBank {Id} locked (exam starts within 24h)", bank.id);
            }
        }

        private async Task ArchiveLockedBanksAsync(DateTime now)
        {
            var archiveCutoff = now.AddDays(-5);

            var lockedBanks = await _repository.GetLockedBanksWithSessionsAsync();

            var toArchive = lockedBanks
                .Where(qb =>
                    qb.ExamSessions.Any() &&
                    qb.ExamSessions.All(es =>
                        es.start_time.AddMinutes(es.duration_minutes) <= archiveCutoff))
                .ToList();

            foreach (var bank in toArchive)
            {
                bank.status = QuestionBankStatus.Archived;
                bank.updated_at = now;
                await _repository.UpdateAsync(bank);
                _logger.LogInformation("QuestionBank {Id} archived (5 days after last exam ended)", bank.id);
            }
        }
    }
}
