using ExamProctoring.Application.Features.Dashboard.DTOs;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Dashboard.Services
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync();
    }
}
