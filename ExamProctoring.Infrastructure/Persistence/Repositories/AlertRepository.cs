using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class AlertRepository : IAlertRepository
    {
        private readonly AppDbContext _context;

        public AlertRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IList<AlertEvent>> GetRecentPagedAsync(int page, int pageSize)
        {
            return await _context.AlertEvents
            .OrderByDescending(a => a.triggered_at)
            .Include(a => a.StudentSession)
            .ThenInclude(ss => ss.Student)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        }

        public async Task<int> CountOpenAlertsAsync()
        {
            return await _context.AlertEvents
                .CountAsync(a => !a.ProctorActions.Any());
        }
    }
}
