using ExamProctoring.Application.Features.Alerts.DTOs;
using ExamProctoring.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Alerts.Services
{
    public interface IAlertService
    {
        IReadOnlyList<AlertTypeDto> GetAlertTypes();

        /// <param name="restrictToProctorId">
        /// When set, results are limited to the sessions that proctor is assigned to.
        /// </param>
        Task<AlertSummaryDto> GetAlertsSummaryAsync(int? restrictToProctorId = null);

        Task<(IEnumerable<AlertEventDto> Items, int TotalCount)> GetAlertsAsync(
            AlertStatus? status,
            string alertType,
            int? sessionId,
            int page,
            int pageSize,
            int? restrictToProctorId = null);

        Task<(bool Success, string Message)> DismissAlertAsync(int alertId, int adminId);

        Task<(bool Success, string Message)> WarnStudentAsync(int alertId, int adminId, string message);

        Task<(bool Success, string Message)> EscalateAlertAsync(int alertId, int adminId, string reason);
    }
}
