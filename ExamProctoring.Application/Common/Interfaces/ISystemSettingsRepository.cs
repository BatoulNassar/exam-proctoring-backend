using ExamProctoring.Domain.Entities;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface ISystemSettingsRepository
    {
        Task<SystemSettings?> GetAsync();
        Task UpdateAsync(SystemSettings settings);

        /// <summary>
        /// Adds the single settings row. Used the first time settings are saved on a
        /// database where the bootstrap seed never ran.
        /// </summary>
        Task AddAsync(SystemSettings settings);
    }
}
