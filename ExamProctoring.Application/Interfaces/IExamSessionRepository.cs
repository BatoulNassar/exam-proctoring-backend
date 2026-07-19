using ExamProctoring.Application.Features.ExamSessions.DTOs;
using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IExamSessionRepository
    {
        Task<IEnumerable<ExamSession>> GetAllSessionsAsync(int page, int pageSize);
        Task<IEnumerable<WeeklyExamSessionStatsDto>> GetWeeklyStatisticsAsync();
    }
}