using ExamProctoring.Domain.Entities;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface ISystemSettingsRepository
    {
        Task<SystemSettings?> GetAsync();
        Task UpdateAsync(SystemSettings settings);
    }
}
