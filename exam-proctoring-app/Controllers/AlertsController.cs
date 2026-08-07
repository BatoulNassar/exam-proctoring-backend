using ExamProctoring.API.Common;
using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Features.Alerts.DTOs;
using ExamProctoring.Application.Features.Alerts.Services;
using ExamProctoring.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.DashboardOnly)]
    [Route("api/alerts")]
    [Authorize]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertsController(IAlertService alertService)
        {
            _alertService = alertService;
        }

        private int? GetActorId()
        {
            var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(actorIdClaim, out var actorId) ? actorId : null;
        }

        /// <summary>
        /// A proctor may only see alerts from the sessions they are assigned to.
        /// Admins and super admins are unrestricted. The scope is derived from the
        /// token, so it cannot be widened by anything in the request.
        /// </summary>
        private int? GetProctorScopeId()
        {
            if (User.IsInRole("SuperAdmin") || User.IsInRole("Admin"))
                return null;

            return User.IsInRole("Proctor") ? GetActorId() : null;
        }

        [HttpGet("types")]
        public IActionResult GetAlertTypes()
        {
            var types = _alertService.GetAlertTypes();
            return Ok(ApiResponse<IReadOnlyList<AlertTypeDto>>.Ok(types, "Alert types retrieved successfully"));
        }

        [HttpGet("Cards")]
        public async Task<IActionResult> GetAlertsCards()
        {
            var summary = await _alertService.GetAlertsSummaryAsync(GetProctorScopeId());
            return Ok(ApiResponse<AlertSummaryDto>.Ok(summary, "Alerts summary retrieved successfully"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAlerts([FromQuery] string? status,[FromQuery] string? alertType,[FromQuery] int? sessionId,[FromQuery] int page = 1,[FromQuery] int pageSize = 10)
        {
            AlertStatus? parsedStatus = null;
            if (!string.IsNullOrEmpty(status) && System.Enum.TryParse<AlertStatus>(status, out var statusEnum))
                parsedStatus = statusEnum;

            var (items, totalCount) = await _alertService.GetAlertsAsync(parsedStatus, alertType, sessionId, page, pageSize, GetProctorScopeId());

            var result = new PagedResult<AlertEventDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<AlertEventDto>>.Ok(result, $"Retrieved {totalCount} alerts"));
        }


        [HttpPost("{alertId}/dismiss")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DismissAlert(int alertId)
        {
            var adminId = GetActorId();
            if (adminId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (success, message) = await _alertService.DismissAlertAsync(alertId, adminId.Value);

            return success
                ? Ok(ApiResponse<object>.Ok(null, message))
                : BadRequest(ApiResponse<object>.Fail(message, 400));
        }



        [HttpPost("{alertId}/warn")]
        [Authorize(Roles = "SuperAdmin,Admin,Proctor")]
        public async Task<IActionResult> WarnStudent(int alertId, [FromBody] WarnStudentRequest request)
        {
            var adminId = GetActorId();
            if (adminId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (success, message) = await _alertService.WarnStudentAsync(alertId, adminId.Value, request.Message);

            return success
                ? Ok(ApiResponse<object>.Ok(null, message))
                : BadRequest(ApiResponse<object>.Fail(message, 400));
        }



        [HttpPost("{alertId}/escalate")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> EscalateAlert(int alertId, [FromBody] EscalateAlertRequest request)
        {
            var adminId = GetActorId();
            if (adminId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (success, message) = await _alertService.EscalateAlertAsync(alertId, adminId.Value, request.Reason);

            return success
                ? Ok(ApiResponse<object>.Ok(null, message))
                : BadRequest(ApiResponse<object>.Fail(message, 400));
        }
    }

    public class WarnStudentRequest
    {
        public string Message { get; set; }
    }

    public class EscalateAlertRequest
    {
        public string Reason { get; set; }
    }
}
