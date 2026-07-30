using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Dashboard.DTOs;
using ExamProctoring.Application.Features.Dashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _dashboardService.GetStatsAsync();
            return Ok(ApiResponse<DashboardStatsDto>.Ok(stats, "Dashboard stats retrieved successfully"));
        }
    }
}
