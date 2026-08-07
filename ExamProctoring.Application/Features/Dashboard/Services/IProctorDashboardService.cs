using ExamProctoring.Application.Features.Dashboard.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Dashboard.Services
{
    /// <summary>
    /// Dashboard data for a single proctor. Every method takes the proctor's own id
    /// and never widens past the sessions they are assigned to.
    /// </summary>
    public interface IProctorDashboardService
    {
        Task<ProctorSummaryCardsDto> GetSummaryCardsAsync(int proctorId);

        Task<IReadOnlyList<AlertTypeCountDto>> GetAlertCountsByTypeAsync(int proctorId, int days);
    }
}
