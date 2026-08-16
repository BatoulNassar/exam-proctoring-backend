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
        private const string IdempotencyKeyHeader = "Idempotency-Key";

        private readonly IMonitoringService _monitoringService;

        public StudentMonitoringController(IMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        [HttpPost("events")]
        public async Task<IActionResult> ReportEvent(
            [FromBody] ReportMonitoringEventRequest request,
            [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey)
        {
            var studentId = int.Parse(User.FindFirst("student_id")!.Value);

            var result = await _monitoringService.ReportEventAsync(request, studentId, idempotencyKey);

            return result.Outcome switch
            {
                ReportMonitoringEventOutcome.AlertRaised =>
                    Ok(ApiResponse<ReportMonitoringEventResult>.Ok(result, "Event recorded and alert raised")),

                ReportMonitoringEventOutcome.RecordedOnly =>
                    Ok(ApiResponse<ReportMonitoringEventResult>.Ok(result, "Event recorded")),

                ReportMonitoringEventOutcome.UnknownEventType =>
                    BadRequest(ApiResponse<object>.Fail(
                        $"Unknown event type '{request.EventType}'", 400, "UNKNOWN_EVENT_TYPE")),

                ReportMonitoringEventOutcome.ValidationError =>
                    BadRequest(ApiResponse<object>.Fail(
                        "Invalid monitoring event payload", 400, "VALIDATION_ERROR")),

                ReportMonitoringEventOutcome.SnapshotTooLarge =>
                    StatusCode(413, ApiResponse<object>.Fail(
                        "Snapshot exceeds the maximum allowed size", 413, "SNAPSHOT_TOO_LARGE")),

                ReportMonitoringEventOutcome.SessionNotFound =>
                    NotFound(ApiResponse<object>.Fail(
                        "Student session not found", 404, "SESSION_NOT_FOUND")),

                ReportMonitoringEventOutcome.NotYourSession =>
                    NotFound(ApiResponse<object>.Fail(
                        "Student session not found", 404, "SESSION_NOT_FOUND")),

                ReportMonitoringEventOutcome.SessionNotActive =>
                    Conflict(ApiResponse<object>.Fail(
                        "This attempt is not currently in progress", 409, "SESSION_NOT_ACTIVE")),

                ReportMonitoringEventOutcome.IdempotencyConflict =>
                    Conflict(ApiResponse<object>.Fail(
                        "Idempotency-Key was reused with a different request", 409, "IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_BODY")),

                _ => StatusCode(500, ApiResponse<object>.Fail("Unexpected result", 500)),
            };
        }
    }
}
