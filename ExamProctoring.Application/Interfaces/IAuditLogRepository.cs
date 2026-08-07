using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<IList<AuditLog>> GetRecentPagedAsync(int page, int pageSize);
        Task<int> CountAsync();

        /// <summary>
        /// Every audit entry, newest first, for a full export. Not paged on purpose:
        /// an export that silently stops at one page is worse than none.
        /// </summary>
        Task<IList<AuditLog>> GetAllForExportAsync();
        Task<IEnumerable<AuditLog>> GetByExamSessionIdAsync(int sessionId);
        Task<Dictionary<int, string>> GetActorNamesByIdsAsync(IEnumerable<int> ids);
        /// Adds and commits immediately. Existing callers rely on this to flush their
        /// own pending changes too, so the behaviour must not change.
        Task AddAsync(AuditLog auditLog);

        /// Adds without committing, leaving the caller's unit of work to save. Use this
        /// when the audit entry must land in the same transaction as the change it
        /// describes.
        Task AddDeferredAsync(AuditLog auditLog);
    }
}
