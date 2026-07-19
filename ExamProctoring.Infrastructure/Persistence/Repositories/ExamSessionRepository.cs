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
            .OrderBy(es => es.start_time)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
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
    }
}