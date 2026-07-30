
using ExamProctoring.Application.Features.ExamSessions.DTOs;

namespace ExamProctoring.Application.Features.ExamSessions.Services
{
    public interface IExamSessionService
    {
        Task<IEnumerable<ExamSessionDto>> GetAllSessionsAsync(int page, int pageSize);
        Task<(CreateExamSessionResult Result, CreateExamSessionResponse? Response)> CreateSessionAsync(CreateExamSessionRequest request, int actorId);
        Task<ExamSessionDetailsDto?> GetSessionDetailsAsync(int id);
        Task<DeleteExamSessionResult> DeleteSessionAsync(int id, int actorId);
        Task<(UpdateExamSessionResult Result, ExamSessionDetailsDto? Session)> UpdateSessionAsync(int id, UpdateExamSessionRequest request, int actorId);
        Task<(EditRestoreSessionResult Result, ExamSessionDetailsDto? Session)> EditRestoreSessionAsync(int id, EditRestoreSessionRequest request, int actorId);
        Task<PublishExamSessionResult> PublishSessionAsync(int id);
        Task<ExtendSessionTimeResult> ExtendSessionTimeAsync(int id, int extraMinutes, int actorId);
        Task<IEnumerable<AvailableProctorDto>> GetAvailableProctorsAsync(System.DateTime sessionStart, System.DateTime sessionEnd);
        Task<IEnumerable<WeeklyExamSessionStatsDto>> GetWeeklyStatisticsAsync();
        Task<string?> ExportGradingReportAsync(int sessionId);
        Task<string?> ExportAuditLogAsync(int sessionId);
    }
}