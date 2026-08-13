using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Dashboard.DTOs;
using ExamProctoring.Application.Features.Dashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

        private int? GetActorId()
        {
            var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(actorIdClaim, out var actorId) ? actorId : null;
        }

        /// <summary>
        /// A SuperAdmin sees the whole university; a plain Admin only sees the
        /// sessions they created. The scope comes from the token, so it cannot be
        /// widened by anything in the request.
        /// </summary>
        private int? GetAdminScopeId() => User.IsInRole("SuperAdmin") ? null : GetActorId();

        /// <summary>
        /// True when the caller is an Admin whose identity claim is unreadable —
        /// serving unscoped numbers in that case would leak other admins' data.
        /// </summary>
        private bool HasUnresolvableScope() => !User.IsInRole("SuperAdmin") && GetActorId() == null;

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            if (HasUnresolvableScope())
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var stats = await _dashboardService.GetStatsAsync(GetAdminScopeId());
            return Ok(ApiResponse<DashboardStatsDto>.Ok(stats, "Dashboard stats retrieved successfully"));
        }

        /// <summary>
        /// The six headline numbers shown on the dashboard cards.
        /// </summary>
        [HttpGet("summary-cards")]
        public async Task<IActionResult> GetSummaryCards()
        {
            if (HasUnresolvableScope())
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var cards = await _dashboardService.GetSummaryCardsAsync(GetAdminScopeId());
            return Ok(ApiResponse<DashboardSummaryCardsDto>.Ok(cards, "Dashboard summary cards retrieved successfully"));
        }

        /// <summary>
        /// Alert counts per alert type. Every known type is returned, zero included.
        /// Not narrowed per admin: alerts are shared across admins, so this chart
        /// covers the same alerts the Alerts page lists.
        /// </summary>
        [HttpGet("alert-counts-by-type")]
        public async Task<IActionResult> GetAlertCountsByType([FromQuery] int days = 7)
        {
            var counts = await _dashboardService.GetAlertCountsByTypeAsync(days);
            return Ok(ApiResponse<IReadOnlyList<AlertTypeCountDto>>.Ok(counts, "Alert counts by type retrieved successfully"));
        }

        /// <summary>
        /// Session and student counts per day. Every day in the range is returned,
        /// zero included, so the chart axis stays continuous.
        /// </summary>
        [HttpGet("session-counts-by-day")]
        public async Task<IActionResult> GetSessionCountsByDay([FromQuery] int days = 7)
        {
            if (HasUnresolvableScope())
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var counts = await _dashboardService.GetSessionCountsByDayAsync(days, GetAdminScopeId());
            return Ok(ApiResponse<IReadOnlyList<SessionCountByDayDto>>.Ok(counts, "Session counts by day retrieved successfully"));
        }

        /// <summary>
        /// Closed sessions split by whether a grading report was already exported.
        /// </summary>
        [HttpGet("session-counts-by-export-status")]
        public async Task<IActionResult> GetSessionCountsByExportStatus()
        {
            if (HasUnresolvableScope())
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var counts = await _dashboardService.GetSessionCountsByExportStatusAsync(GetAdminScopeId());
            return Ok(ApiResponse<SessionExportStatusDto>.Ok(counts, "Session counts by export status retrieved successfully"));
        }

        /// <summary>
        /// Question count per question bank. Banks are a shared university-wide
        /// catalog, so this one is not narrowed per admin.
        /// </summary>
        [HttpGet("question-counts-by-bank")]
        public async Task<IActionResult> GetQuestionCountsByBank()
        {
            var counts = await _dashboardService.GetQuestionCountsByBankAsync();
            return Ok(ApiResponse<IReadOnlyList<QuestionCountByBankDto>>.Ok(counts, "Question counts by bank retrieved successfully"));
        }
    }
}
