
using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Features.ExamSessions.DTOs;

namespace ExamProctoring.Application.Features.ExamSessions.Services
{
    public interface IExamSessionService
    {
        /// <param name="adminId">
        /// Owner scope: null shows every session (SuperAdmin), a value shows only the
        /// sessions that admin created.
        /// </param>
        Task<PagedResult<ExamSessionDto>> GetAllSessionsAsync(int page, int pageSize, int? adminId = null);
        Task<(CreateExamSessionResult Result, CreateExamSessionResponse? Response)> CreateSessionAsync(CreateExamSessionRequest request, int actorId);
        /// <param name="adminId">
        /// Owner scope. When set and the session belongs to another admin, this
        /// returns null so the caller answers 404.
        /// </param>
        Task<ExamSessionDetailsDto?> GetSessionDetailsAsync(int id, int? adminId = null);
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