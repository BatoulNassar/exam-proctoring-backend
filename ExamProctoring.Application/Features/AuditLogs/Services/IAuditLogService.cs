using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Features.AuditLogs.DTOs;

namespace ExamProctoring.Application.Features.AuditLogs.Services
{
    public interface IAuditLogService
    {
        Task<PagedResult<AuditLogDto>> GetRecentAuditsAsync(int page, int pageSize);

        /// <summary>The whole audit log as CSV, newest first.</summary>
        Task<string> ExportCsvAsync();

        /// <summary>
        /// Records one audited action. The row is added but NOT committed: the caller
        /// saves, so the audit entry and the change it describes land in the same
        /// transaction and cannot drift apart.
        /// </summary>
        Task LogAsync(int examSessionId, int actorId, string actorType, string action, int entityId, string entityType, string details);
    }
}