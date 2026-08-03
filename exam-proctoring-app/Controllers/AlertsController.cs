using ExamProctoring.API.Common; 
using ExamProctoring.Application.Features.Alerts.DTOs;
using ExamProctoring.Application.Features.Alerts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    [Route("api/alerts")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.DashboardOnly)]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertsController(IAlertService alertService)
        {
            _alertService = alertService;
        }


        [HttpGet]
        public async Task<IActionResult> GetRecentAlerts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var alerts = await _alertService.GetRecentAlertsAsync(page, pageSize);

            return Ok(ApiResponse<IEnumerable<AlertEventDto>>.Ok(alerts, "Alerts retrieved successfully"));
        }
    }
}