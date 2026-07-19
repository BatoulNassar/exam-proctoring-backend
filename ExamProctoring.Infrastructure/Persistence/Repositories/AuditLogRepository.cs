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
            .OrderByDescending(a => a.occurred_at)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        }
    }
}
