using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<IList<AuditLog>> GetRecentPagedAsync(int page, int pageSize);
        Task<IEnumerable<AuditLog>> GetByExamSessionIdAsync(int sessionId);
        Task AddAsync(AuditLog auditLog);
    }
}
