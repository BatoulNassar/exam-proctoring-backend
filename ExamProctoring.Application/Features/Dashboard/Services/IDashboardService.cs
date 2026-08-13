using ExamProctoring.Application.Features.Dashboard.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Dashboard.Services
{
    /// <summary>
    /// Admin dashboard aggregates.
    /// <para>
    /// Session-based figures take an owner scope: <c>null</c> means the caller sees
    /// every session (SuperAdmin), a value limits them to sessions created by that
    /// admin. The controller derives it from the token, so a request can never
    /// widen it.
    /// </para>
    /// <para>
    /// Alert figures take no scope at all. An admin may be proctoring a session
    /// someone else created, so alerts are shared across admins — and this keeps the
    /// dashboard agreeing with /api/alerts, which has always been unrestricted for
    /// them.
    /// </para>
    /// </summary>
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync(int? adminId);

        Task<DashboardSummaryCardsDto> GetSummaryCardsAsync(int? adminId);

        Task<IReadOnlyList<AlertTypeCountDto>> GetAlertCountsByTypeAsync(int days);

        Task<IReadOnlyList<SessionCountByDayDto>> GetSessionCountsByDayAsync(int days, int? adminId);

        Task<SessionExportStatusDto> GetSessionCountsByExportStatusAsync(int? adminId);

        Task<IReadOnlyList<QuestionCountByBankDto>> GetQuestionCountsByBankAsync();
    }
}
