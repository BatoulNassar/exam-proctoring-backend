using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Dashboard.DTOs;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Applies the owner scope to a session query. A null adminId means the
        /// caller is allowed to see every session (SuperAdmin).
        /// </summary>
        private static IQueryable<ExamSession> ScopeToAdmin(IQueryable<ExamSession> query, int? adminId) =>
            adminId.HasValue ? query.Where(e => e.created_by_admin_id == adminId.Value) : query;

        /// <summary>
        /// Same owner scope, reached through the student session that the alert
        /// belongs to.
        /// </summary>
        private static IQueryable<AlertEvent> ScopeToAdmin(IQueryable<AlertEvent> query, int? adminId) =>
            adminId.HasValue
                ? query.Where(a => a.StudentSession.ExamSession.created_by_admin_id == adminId.Value)
                : query;

        public async Task<int> CountAllSessionsAsync(int? adminId = null)
        {
            return await ScopeToAdmin(_context.ExamSessions, adminId).CountAsync();
        }

        public async Task<int> CountActiveSessionsAsync(int? adminId = null)
        {
            return await ScopeToAdmin(_context.ExamSessions, adminId)
                .CountAsync(e => e.status == ExamSessionStatus.ACTIVE);
        }

        public async Task<int> CountStudentsInExamAsync(int? adminId = null)
        {
            var query = _context.StudentSessions.Where(ss => ss.status == StudentSessionStatus.InExam);

            if (adminId.HasValue)
                query = query.Where(ss => ss.ExamSession.created_by_admin_id == adminId.Value);

            return await query.CountAsync();
        }

        public async Task<int> CountRegisteredStudentsAsync()
        {
            return await _context.Students.CountAsync();
        }

        public async Task<int> CountAlertsByStatusAndSeverityAsync(AlertStatus status, AlertSeverity? severity, int? adminId = null)
        {
            var query = ScopeToAdmin(_context.AlertEvents, adminId).Where(a => a.status == status);

            if (severity.HasValue)
                query = query.Where(a => a.severity == severity.Value);

            return await query.CountAsync();
        }

        public async Task<SessionExportStatusDto> GetClosedSessionExportCountsAsync(int? adminId = null)
        {
            // "Closed" here means CLOSED only: an ARCHIVED session is already filed
            // away and should not read as outstanding export work.
            var counts = await ScopeToAdmin(_context.ExamSessions, adminId)
                .Where(e => e.status == ExamSessionStatus.CLOSED)
                .GroupBy(e => _context.GradingExports.Any(g => g.exam_session_id == e.id))
                .Select(g => new { HasExport = g.Key, Count = g.Count() })
                .ToListAsync();

            return new SessionExportStatusDto
            {
                Exported = counts.FirstOrDefault(c => c.HasExport)?.Count ?? 0,
                Pending = counts.FirstOrDefault(c => !c.HasExport)?.Count ?? 0
            };
        }

        public async Task<int> CountAlertsInSessionsAsync(IReadOnlyCollection<int> sessionIds, AlertStatus status, DateTime? since)
        {
            if (sessionIds == null || sessionIds.Count == 0) return 0;

            var ids = sessionIds.ToList();

            var query = _context.AlertEvents
                .Where(a => ids.Contains(a.StudentSession.exam_session_id) && a.status == status);

            if (since.HasValue)
            {
                // AlertEvent has no resolved_at column, so the proctor action that
                // closed the alert is what dates the state change.
                query = query.Where(a => a.ProctorActions.Any(pa => pa.acted_at >= since.Value));
            }

            return await query.CountAsync();
        }

        public async Task<int> CountActiveSessionsAmongAsync(IReadOnlyCollection<int> sessionIds)
        {
            if (sessionIds == null || sessionIds.Count == 0) return 0;

            var ids = sessionIds.ToList();

            return await _context.ExamSessions.CountAsync(e =>
                ids.Contains(e.id) &&
                (e.status == ExamSessionStatus.ACTIVE || e.status == ExamSessionStatus.GRACE));
        }

        public async Task<IReadOnlyDictionary<string, int>> GetAlertCountsByTypeAsync(DateTime from, IReadOnlyCollection<int> sessionIds = null, int? adminId = null)
        {
            var query = ScopeToAdmin(_context.AlertEvents, adminId).Where(a => a.triggered_at >= from);

            if (sessionIds != null)
            {
                if (sessionIds.Count == 0)
                    return new Dictionary<string, int>();

                var ids = sessionIds.ToList();
                query = query.Where(a => ids.Contains(a.StudentSession.exam_session_id));
            }

            var counts = await query
                .GroupBy(a => a.alert_type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts
                .Where(c => c.Type != null)
                .ToDictionary(c => c.Type, c => c.Count);
        }

        public async Task<IReadOnlyList<SessionCountByDayDto>> GetSessionCountsByDayAsync(DateTime from, DateTime toExclusive, int? adminId = null)
        {
            var perSession = await ScopeToAdmin(_context.ExamSessions, adminId)
                .Where(e => e.start_time >= from && e.start_time < toExclusive)
                .Select(e => new
                {
                    e.start_time,
                    Students = e.StudentSessions.Count(s => s.status != StudentSessionStatus.NotStarted)
                })
                .ToListAsync();

            return perSession
                .GroupBy(e => e.start_time.Date)
                .Select(g => new SessionCountByDayDto
                {
                    // Tagged as UTC so these serialize identically to the empty days
                    // the service fills in; otherwise the client sees two date formats.
                    Date = DateTime.SpecifyKind(g.Key, DateTimeKind.Utc),
                    DayName = g.Key.DayOfWeek.ToString(),
                    SessionCount = g.Count(),
                    StudentCount = g.Sum(x => x.Students)
                })
                .ToList();
        }

        public async Task<IReadOnlyList<QuestionCountByBankDto>> GetQuestionCountsByBankAsync()
        {
            return await _context.QuestionBanks
                .Select(b => new QuestionCountByBankDto
                {
                    BankId = b.id,
                    CourseCode = b.course_code,
                    BankTitle = b.title,
                    QuestionCount = b.Questions.Count()
                })
                .OrderBy(b => b.CourseCode)
                .ToListAsync();
        }
    }
}
