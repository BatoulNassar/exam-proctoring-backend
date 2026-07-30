using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.ExamSessions.DTOs;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
   
    public class ExamSessionRepository : IExamSessionRepository
    {
        private readonly AppDbContext _context;

        public ExamSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ExamSession>> GetAllSessionsAsync(int page, int pageSize)
        {
            return await _context.ExamSessions
            .Include(es => es.QuestionBank)
            .Include(es => es.StudentSessions)
            .Include(es => es.ProctorSessions)
                .ThenInclude(ps => ps.Proctor)
            .OrderBy(es => es.start_time)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        }
        public async Task<ExamSession?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.ExamSessions
                .Include(es => es.QuestionBank)
                    .ThenInclude(qb => qb.Questions)
                .Include(es => es.CreatedByAdmin)
                .AsNoTracking()
                .SingleOrDefaultAsync(es => es.id == id);
        }

        public async Task<ExamSession?> GetByIdAsync(int id)
        {
            return await _context.ExamSessions
                .SingleOrDefaultAsync(es => es.id == id);
        }

        public async Task<ExamSession?> GetByIdWithQuestionBankAsync(int id)
        {
            return await _context.ExamSessions
                .Include(es => es.QuestionBank)
                    .ThenInclude(qb => qb.Questions)
                .SingleOrDefaultAsync(es => es.id == id);
        }

        public async Task AddAsync(ExamSession session)
        {
            await _context.ExamSessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        public async Task AddStudentSessionsAsync(IEnumerable<StudentSession> studentSessions)
        {
            await _context.StudentSessions.AddRangeAsync(studentSessions);
            await _context.SaveChangesAsync();
        }

        public async Task AddProctorSessionsAsync(IEnumerable<ProctorSession> proctorSessions)
        {
            await _context.ProctorSessions.AddRangeAsync(proctorSessions);
            await _context.SaveChangesAsync();
        }

        public async Task<ExamSession?> GetByIdWithProctorsAsync(int id)
        {
            return await _context.ExamSessions
                .Include(es => es.ProctorSessions)
                    .ThenInclude(ps => ps.Proctor)
                .Include(es => es.StudentSessions)
                    .ThenInclude(ss => ss.Student)
                .Include(es => es.QuestionBank)
                .SingleOrDefaultAsync(es => es.id == id);
        }

        public async Task RemoveProctorSessionAsync(int examSessionId, int proctorId)
        {
            var proctorSession = await _context.ProctorSessions
                .SingleOrDefaultAsync(ps => ps.exam_session_id == examSessionId && ps.proctor_id == proctorId);

            if (proctorSession != null)
            {
                _context.ProctorSessions.Remove(proctorSession);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveStudentSessionAsync(StudentSession studentSession)
        {
            _context.StudentSessions.Remove(studentSession);
            await _context.SaveChangesAsync();
        }

        public async Task<List<StudentSession>> GetStudentSessionsByIdAsync(int examSessionId, int[] studentIds)
        {
            return await _context.StudentSessions
                .Where(ss => ss.exam_session_id == examSessionId && studentIds.Contains(ss.student_id))
                .ToListAsync();
        }

        public async Task UpdateAsync(ExamSession session)
        {
            _context.ExamSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ProctorSession>> GetProctorSessionsWithTimeConflictAsync(int proctorId, DateTime startTime, DateTime endTime)
        {
            return await _context.ProctorSessions
                .Include(ps => ps.ExamSession)
                .Where(ps => ps.proctor_id == proctorId &&
                    ps.ExamSession.status != ExamSessionStatus.CLOSED &&
                    ps.ExamSession.status != ExamSessionStatus.ARCHIVED &&
                    ps.ExamSession.start_time < endTime &&
                    ps.ExamSession.start_time.AddMinutes(ps.ExamSession.duration_minutes) > startTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExamSession>> GetByStatusAsync(ExamSessionStatus status, System.Linq.Expressions.Expression<System.Func<ExamSession, bool>>? predicate = null)
        {
            var query = _context.ExamSessions.Where(es => es.status == status);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ExamSession>> GetByStatusesAsync(ExamSessionStatus[] statuses, System.Linq.Expressions.Expression<System.Func<ExamSession, bool>>? predicate = null)
        {
            var query = _context.ExamSessions.Where(es => statuses.Contains(es.status));
            if (predicate != null)
                query = query.Where(predicate);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<WeeklyExamSessionStatsDto>> GetWeeklyStatisticsAsync()
        {
            var today = DateTime.Today;

            int daysSinceSaturday = ((int)today.DayOfWeek + 1) % 7;
            var weekStart = today.AddDays(-daysSinceSaturday);

            var weekEnd = weekStart.AddDays(7);

            var statistics = await _context.ExamSessions
                .Where(e => e.start_time >= weekStart && e.start_time < weekEnd)
                .Select(e => new
                {
                    Day = e.start_time.DayOfWeek,
                    Students = e.StudentSessions.Count(s =>
                        s.status != StudentSessionStatus.NotStarted)
                })
                .ToListAsync();

            var result = statistics
                .GroupBy(x => x.Day)
                .ToDictionary(
                    g => g.Key,
                    g => new WeeklyExamSessionStatsDto
                    {
                        Day = g.Key.ToString(),
                        TotalSessions = g.Count(),
                        TotalStudents = g.Sum(x => x.Students)
                    });

            return result.Values;
        }

        public async Task<int> CountActiveSessionsAsync()
        {
            return await _context.ExamSessions
                .CountAsync(es => es.status == ExamSessionStatus.ACTIVE);
        }

        public async Task<int> CountStudentsInExamAsync()
        {
            return await _context.StudentSessions
                .CountAsync(ss => ss.status == StudentSessionStatus.InExam);
        }
    }
}