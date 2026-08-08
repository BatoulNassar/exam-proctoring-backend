using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Settings.DTOs;
using ExamProctoring.Domain.Enums;

namespace ExamProctoring.Application.Features.Settings.Services
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly ISystemSettingsRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public SystemSettingsService(ISystemSettingsRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SystemSettingsDto?> GetSettingsAsync()
        {
            var settings = await _repository.GetAsync();
            if (settings == null) return null;

            return new SystemSettingsDto
            {
                GazeAlertThresholdSec = settings.gaze_alert_threshold_sec,
                FaceSensitivity = settings.face_sensitivity.ToString(),
                AmbientAudioMonitoring = settings.ambient_audio_monitoring,
                GracePeriodMinutes = settings.grace_period_minutes,
                LoginWindowMinutes = settings.login_window_minutes,
                MaxLivenessAttempts = settings.max_liveness_attempts,
                FaceMatchThreshold = settings.face_match_threshold,
                QuestionRandomisation = settings.question_randomisation,
                OptionShuffle = settings.option_shuffle,
                MaxWarningsBeforeTermination = settings.max_warnings_before_termination
            };
        }

        public async Task<(bool Success, string Message, SystemSettingsDto? UpdatedData)> UpdateSettingsAsync(SystemSettingsDto dto, int updatedBy)
        {
            if (!Enum.TryParse<FaceSensitivity>(dto.FaceSensitivity, out var sensitivity))
                return (false, $"Invalid face sensitivity value: {dto.FaceSensitivity}. Allowed: Low, Medium, High", null);

            if (dto.MaxWarningsBeforeTermination < 0)
                return (false, "Max warnings before termination cannot be negative", null);

            // PUT on a singleton resource creates it when absent: settings are one row
            // for the whole system, so the first save on a database whose bootstrap
            // seed never ran is a create, not a "not found".
            var settings = await _repository.GetAsync();
            var isNew = settings == null;

            if (isNew)
            {
                settings = new Domain.Entities.SystemSettings { created_at = DateTime.UtcNow, created_by = updatedBy };
            }

            settings.gaze_alert_threshold_sec = dto.GazeAlertThresholdSec;
            settings.face_sensitivity = sensitivity;
            settings.ambient_audio_monitoring = dto.AmbientAudioMonitoring;
            settings.grace_period_minutes = dto.GracePeriodMinutes;
            settings.login_window_minutes = dto.LoginWindowMinutes;
            settings.max_liveness_attempts = dto.MaxLivenessAttempts;
            settings.face_match_threshold = dto.FaceMatchThreshold;
            settings.question_randomisation = dto.QuestionRandomisation;
            settings.option_shuffle = dto.OptionShuffle;
            settings.max_warnings_before_termination = dto.MaxWarningsBeforeTermination;

            if (isNew)
            {
                await _repository.AddAsync(settings);
            }
            else
            {
                settings.updated_at = DateTime.UtcNow;
                settings.updated_by = updatedBy;
                await _repository.UpdateAsync(settings);
            }

            await _unitOfWork.SaveChangesAsync();

            return (true, isNew ? "Settings created successfully" : "Settings updated successfully", dto);
        }
    }
}