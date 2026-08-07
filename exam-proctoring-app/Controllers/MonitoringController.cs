using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Monitoring.DTOs;
using ExamProctoring.Application.Features.Monitoring.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Route("api/monitoring")]
    // DashboardOnly rather than a bare [Authorize]: student desktop tokens are valid
    // JWTs and would otherwise reach these endpoints and read the whole roster.
    [Authorize(Policy = AuthorizationPolicies.DashboardOnly)]
    public class MonitoringController : ControllerBase
    {
        private readonly IMonitoringService _monitoringService;

        public MonitoringController(IMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        private int? GetActorId()
        {
            var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(actorIdClaim, out var actorId) ? actorId : null;
        }

        /// <summary>
        /// A proctor is limited to the sessions they are assigned to; admins and
        /// super admins are unrestricted. Taken from the token so nothing in the
        /// request can widen it.
        /// </summary>
        private int? GetProctorScopeId()
        {
            if (User.IsInRole("SuperAdmin") || User.IsInRole("Admin"))
                return null;

            return User.IsInRole("Proctor") ? GetActorId() : null;
        }

        [HttpGet("active-sessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var sessions = await _monitoringService.GetActiveSessionsAsync(GetProctorScopeId());
            return Ok(ApiResponse<IEnumerable<ActiveSessionDto>>.Ok(sessions, "Active sessions retrieved successfully"));
        }

        [HttpGet("sessions/{sessionId}/students")]
        public async Task<IActionResult> GetSessionStudents(int sessionId)
        {
            var (allowed, students) = await _monitoringService.GetSessionStudentsAsync(sessionId, GetProctorScopeId());

            if (!allowed)
                return StatusCode(403, ApiResponse<object>.Fail("You are not assigned to this exam session", 403));

            return Ok(ApiResponse<IEnumerable<StudentMonitoringDto>>.Ok(students, "Session students retrieved successfully"));
        }
    }
}
