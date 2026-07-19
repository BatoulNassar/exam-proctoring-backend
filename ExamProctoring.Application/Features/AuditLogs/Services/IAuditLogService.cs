using ExamProctoring.Application.Features.AuditLogs.DTOs;

namespace ExamProctoring.Application.Features.AuditLogs.Services
{
    public interface IAuditLogService
    {
        Task<IEnumerable<AuditLogDto>> GetRecentAuditsAsync(int page, int pageSize);
    }
}