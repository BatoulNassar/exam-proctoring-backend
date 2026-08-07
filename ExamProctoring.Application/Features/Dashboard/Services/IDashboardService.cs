using ExamProctoring.Application.Features.Dashboard.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Dashboard.Services
{
    /// <summary>
    /// Admin dashboard aggregates. Every method takes an owner scope:
    /// <c>null</c> means the caller sees every session (SuperAdmin), a value
    /// limits the numbers to sessions created by that admin. The controller
    /// derives it from the token, so a request can never widen it.
    /// </summary>
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync(int? adminId);

        Task<DashboardSummaryCardsDto> GetSummaryCardsAsync(int? adminId);

        Task<IReadOnlyList<AlertTypeCountDto>> GetAlertCountsByTypeAsync(int days, int? adminId);

        Task<IReadOnlyList<SessionCountByDayDto>> GetSessionCountsByDayAsync(int days, int? adminId);

        Task<SessionExportStatusDto> GetSessionCountsByExportStatusAsync(int? adminId);

        Task<IReadOnlyList<QuestionCountByBankDto>> GetQuestionCountsByBankAsync();
    }
}
