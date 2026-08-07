using ExamProctoring.Application.Features.Settings.DTOs;

namespace ExamProctoring.Application.Features.Settings.Services
{
    public interface ISystemSettingsService
    {
        Task<SystemSettingsDto?> GetSettingsAsync();
        Task<(bool Success, string Message, SystemSettingsDto? UpdatedData)> UpdateSettingsAsync(SystemSettingsDto dto, int updatedBy);
    }
}
