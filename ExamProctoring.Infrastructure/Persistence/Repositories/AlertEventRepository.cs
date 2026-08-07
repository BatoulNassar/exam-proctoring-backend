using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class AlertEventRepository : IAlertEventRepository
    {
        private readonly AppDbContext _context;

        public AlertEventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AlertEvent> GetByIdAsync(int id)
        {
            return await _context.AlertEvents
                .Include(a => a.StudentSession)
                    .ThenInclude(ss => ss.Student)
                .Include(a => a.StudentSession)
                    .ThenInclude(ss => ss.ExamSession)
                .Include(a => a.ProctorActions)
                .FirstOrDefaultAsync(a => a.id == id);
        }

        public async Task<IEnumerable<AlertEvent>> GetAllAsync()
        {
            return await _context.AlertEvents
                .Include(a => a.StudentSession)
                    .ThenInclude(ss => ss.Student)
                .Include(a => a.StudentSession)
                    .ThenInclude(ss => ss.ExamSession)
                .Include(a => a.ProctorActions)
                .OrderByDescending(a => a.triggered_at)
                .ToListAsync();
        }

        public async Task<IEnumerable<AlertEvent>> GetAlertsByFilterAsync(AlertStatus? status, string alertType, int? sessionId, IReadOnlyCollection<int> restrictToSessionIds = null)
        {
            var query = _context.AlertEvents
                .Include(a => a.StudentSession)
                    .ThenInclude(ss => ss.Student)
                .Include(a => a.StudentSession)
                    .ThenInclude(ss => ss.ExamSession)
                .Include(a => a.ProctorActions)
                .AsQueryable();

            // Applied before the user-supplied filters: a caller can narrow this
            // down, never widen past it.
            if (restrictToSessionIds != null)
            {
                if (restrictToSessionIds.Count == 0)
                    return new List<AlertEvent>();

                var allowed = restrictToSessionIds.ToList();
                query = query.Where(a => allowed.Contains(a.StudentSession.exam_session_id));
            }

            if (status.HasValue)
                query = query.Where(a => a.status == status.Value);

            if (!string.IsNullOrEmpty(alertType))
                query = query.Where(a => a.alert_type == alertType);

            if (sessionId.HasValue)
                query = query.Where(a => a.StudentSession.exam_session_id == sessionId.Value);

            return await query.OrderByDescending(a => a.triggered_at).ToListAsync();
        }

        public async Task AddAsync(AlertEvent alert)
        {
            await _context.AlertEvents.AddAsync(alert);
        }

        public async Task UpdateAsync(AlertEvent alert)
        {
            _context.AlertEvents.Update(alert);
            await Task.CompletedTask;
        }
    }
}
