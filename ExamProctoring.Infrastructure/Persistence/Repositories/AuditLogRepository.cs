using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AppDbContext _context;

        public AuditLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IList<AuditLog>> GetRecentPagedAsync(int page, int pageSize)
        {
            return await _context.AuditLogs
                .Include(a => a.ExamSession)
                .OrderByDescending(a => a.occurred_at)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.AuditLogs.CountAsync();
        }

        public async Task<IList<AuditLog>> GetAllForExportAsync()
        {
            return await _context.AuditLogs
                .Include(a => a.ExamSession)
                .OrderByDescending(a => a.occurred_at)
                .ThenByDescending(a => a.id)
                .ToListAsync();
        }

        public async Task<Dictionary<int, string>> GetActorNamesByIdsAsync(IEnumerable<int> ids)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .Where(u => ids.Contains(u.id))
                .ToDictionaryAsync(u => u.id, u => u.full_name);
        }

        public async Task<IEnumerable<AuditLog>> GetByExamSessionIdAsync(int sessionId)
        {
            return await _context.AuditLogs
            .Where(a => a.exam_session_id == sessionId)
            .OrderBy(a => a.occurred_at)
            .ToListAsync();
        }

        public async Task AddAsync(AuditLog auditLog)
        {
            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task AddDeferredAsync(AuditLog auditLog)
        {
            await _context.AuditLogs.AddAsync(auditLog);
        }
    }
}
