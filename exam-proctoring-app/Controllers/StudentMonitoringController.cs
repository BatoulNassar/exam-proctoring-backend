using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Monitoring.DTOs;
using ExamProctoring.Application.Features.Monitoring.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    /// <summary>
    /// Called by the student desktop client to report what its monitoring detected.
    /// Student tokens only — a dashboard user has no business reporting events, and
    /// the StudentOnly policy rejects them.
    /// </summary>
    [ApiController]
    [Route("api/v1/monitoring")]
    [Authorize(Policy = AuthorizationPolicies.StudentOnly)]
    public class StudentMonitoringController : ControllerBase
    {
        private readonly IMonitoringService _monitoringService;

        public StudentMonitoringController(IMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        [HttpPost("events")]
        public async Task<IActionResult> ReportEvent([FromBody] ReportMonitoringEventRequest request)
        {
            // The StudentOnly policy guarantees a positive integer student_id claim.
            var studentId = int.Parse(User.FindFirst("student_id")!.Value);

            var result = await _monitoringService.ReportEventAsync(request, studentId);

            return result.Outcome switch
            {
                ReportMonitoringEventOutcome.AlertRaised =>
                    Ok(ApiResponse<ReportMonitoringEventResult>.Ok(result, "Event recorded and alert raised")),

                ReportMonitoringEventOutcome.RecordedOnly =>
                    Ok(ApiResponse<ReportMonitoringEventResult>.Ok(result, "Event recorded")),

                ReportMonitoringEventOutcome.UnknownEventType =>
                    BadRequest(ApiResponse<object>.Fail($"Unknown event type '{request.EventType}'", 400)),

                ReportMonitoringEventOutcome.SessionNotFound =>
                    NotFound(ApiResponse<object>.Fail("Student session not found", 404)),

                // Treated as not found: probing ids should not reveal other students' attempts.
                ReportMonitoringEventOutcome.NotYourSession =>
                    NotFound(ApiResponse<object>.Fail("Student session not found", 404)),

                ReportMonitoringEventOutcome.SessionNotActive =>
                    BadRequest(ApiResponse<object>.Fail("This attempt is not currently in progress", 400)),

                _ => StatusCode(500, ApiResponse<object>.Fail("Unexpected result", 500)),
            };
        }
    }
}
