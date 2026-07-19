
using ExamProctoring.Application.Features.ExamSessions.DTOs;

namespace ExamProctoring.Application.Features.ExamSessions.Services
{
    public interface IExamSessionService
    {
        Task<IEnumerable<ExamSessionDto>> GetAllSessionsAsync(int page, int pageSize);
        Task<IEnumerable<WeeklyExamSessionStatsDto>> GetWeeklyStatisticsAsync();
    }
}