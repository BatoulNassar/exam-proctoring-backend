using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Dashboard.DTOs;
using ExamProctoring.Application.Features.Dashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ExamProctoring.API.Controllers
{
    /// <summary>
    /// Dashboard for the signed-in proctor. Kept apart from the admin dashboard on
    /// purpose: everything here is limited to the proctor's assigned sessions, and
    /// the scope comes from the token, never from a query parameter.
    /// </summary>
    [ApiController]
    [Route("api/proctor-dashboard")]
    [Authorize(Roles = "Proctor")]
    public class ProctorDashboardController : ControllerBase
    {
        private readonly IProctorDashboardService _proctorDashboardService;

        public ProctorDashboardController(IProctorDashboardService proctorDashboardService)
        {
            _proctorDashboardService = proctorDashboardService;
        }

        private int? GetActorId()
        {
            var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(actorIdClaim, out var actorId) ? actorId : null;
        }

        /// <summary>
        /// Active sessions, open alerts and alerts resolved today, for this proctor.
        /// </summary>
        [HttpGet("summary-cards")]
        public async Task<IActionResult> GetSummaryCards()
        {
            var proctorId = GetActorId();
            if (proctorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var cards = await _proctorDashboardService.GetSummaryCardsAsync(proctorId.Value);
            return Ok(ApiResponse<ProctorSummaryCardsDto>.Ok(cards, "Proctor summary cards retrieved successfully"));
        }

        /// <summary>
        /// Alert counts per type inside this proctor's sessions. Every known type is
        /// returned, zero included.
        /// </summary>
        [HttpGet("alert-counts-by-type")]
        public async Task<IActionResult> GetAlertCountsByType([FromQuery] int days = 7)
        {
            var proctorId = GetActorId();
            if (proctorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var counts = await _proctorDashboardService.GetAlertCountsByTypeAsync(proctorId.Value, days);
            return Ok(ApiResponse<IReadOnlyList<AlertTypeCountDto>>.Ok(counts, "Alert counts by type retrieved successfully"));
        }
    }
}
