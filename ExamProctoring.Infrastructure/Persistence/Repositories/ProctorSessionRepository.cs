using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class ProctorSessionRepository : IProctorSessionRepository
    {
        private readonly AppDbContext _context;

        public ProctorSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProctorSession>> GetByExamSessionIdAsync(int examSessionId)
        {
            return await _context.ProctorSessions
                .Where(ps => ps.exam_session_id == examSessionId)
                .Include(ps => ps.Proctor)
                .ToListAsync();
        }

        public async Task<ProctorSession?> GetByExamSessionAndProctorAsync(int examSessionId, int proctorId)
        {
            return await _context.ProctorSessions
                .SingleOrDefaultAsync(ps => ps.exam_session_id == examSessionId && ps.proctor_id == proctorId);
        }

        public async Task<IReadOnlyList<int>> GetSessionIdsByProctorAsync(int proctorId)
        {
            return await _context.ProctorSessions
                .Where(ps => ps.proctor_id == proctorId)
                .Select(ps => ps.exam_session_id)
                .Distinct()
                .ToListAsync();
        }

        public async Task<(IReadOnlyList<dynamic> Sessions, int TotalCount)> GetProctorSessionsPagedAsync(int proctorId, int page, int pageSize)
        {
            var sessionIds = await GetSessionIdsByProctorAsync(proctorId);
            if (sessionIds.Count == 0)
                return (new List<dynamic>(), 0);

            var ids = sessionIds.ToList();

            var totalCount = await _context.ExamSessions
                .Where(e => ids.Contains(e.id))
                .CountAsync();

            var sessions = await _context.ExamSessions
                .Where(e => ids.Contains(e.id))
                .OrderByDescending(e => e.start_time)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.id,
                    e.title,
                    e.course_tag,
                    Status = e.status.ToString(),
                    e.start_time,
                    e.duration_minutes,
                    ActiveStudents = e.StudentSessions.Count(s => s.status == Domain.Enums.StudentSessionStatus.InExam),
                    TotalEnrolledStudents = e.StudentSessions.Count(s => s.status != Domain.Enums.StudentSessionStatus.NotStarted),
                    OpenAlerts = e.StudentSessions.SelectMany(ss => ss.Alerts).Count(a => a.status == Domain.Enums.AlertStatus.Open)
                })
                .ToListAsync();

            return (sessions.Cast<dynamic>().ToList(), totalCount);
        }

        public async Task AddAsync(ProctorSession proctorSession)
        {
            await _context.ProctorSessions.AddAsync(proctorSession);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(ProctorSession proctorSession)
        {
            _context.ProctorSessions.Remove(proctorSession);
            await _context.SaveChangesAsync();
        }
    }
}
