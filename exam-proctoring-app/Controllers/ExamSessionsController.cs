using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.ExamSessions.DTOs;
using ExamProctoring.Application.Features.ExamSessions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Route("api/exam-sessions")]
    public class ExamSessionsController : ControllerBase
    {
        private readonly IExamSessionService _examSessionService;
        private readonly IExamSessionStateTransitionService _transitionService;

        public ExamSessionsController(IExamSessionService examSessionService,
IExamSessionStateTransitionService transitionService)
        {
            _examSessionService = examSessionService;
            _transitionService = transitionService;
        }

        private int? GetActorId()
        {
            var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(actorIdClaim, out var actorId) ? actorId : null;
        }

        private bool IsSuperAdmin()
        {
            return User.IsInRole("SuperAdmin");
        }

        /// <summary>
        /// Creates a new exam session as a draft, optionally enrolling students from a roster CSV.
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateSession([FromForm] CreateExamSessionRequest request)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (result, response) = await _examSessionService.CreateSessionAsync(request, actorId.Value);

            return result switch
            {
                CreateExamSessionResult.InvalidSettings => BadRequest(ApiResponse<object>.Fail("Invalid session settings", 400)),
                CreateExamSessionResult.StartTimeInPast => BadRequest(ApiResponse<object>.Fail("Scheduled start must be in the future", 400)),
                CreateExamSessionResult.QuestionBankNotFound => BadRequest(ApiResponse<object>.Fail("The selected question bank does not exist", 400)),
                CreateExamSessionResult.InvalidCsv => BadRequest(ApiResponse<object>.Fail("The students CSV file contains no valid rows", 400)),
                CreateExamSessionResult.InvalidProctor => BadRequest(ApiResponse<object>.Fail("One or more assigned proctors are invalid or do not have Proctor role", 400)),
                CreateExamSessionResult.ProctorNotAvailable => BadRequest(ApiResponse<object>.Fail("One or more assigned proctors have scheduling conflicts", 400)),
                _ => CreatedAtAction(
                        nameof(GetSessionDetails),
                        new { id = response!.Session.Id },
                        ApiResponse<CreateExamSessionResponse>.Ok(response, "Exam session created successfully", 201)),
            };
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetAllSessionsPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 4)
        {
            var sessions = await _examSessionService.GetAllSessionsAsync(page, pageSize);
            return Ok(ApiResponse<IEnumerable<ExamSessionDto>>.Ok(sessions, "The sessions were fetched successfully"));
        }

        /// <summary>
        /// Returns full details of a single exam session (read-only view).
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSessionDetails(int id)
        {
            var session = await _examSessionService.GetSessionDetailsAsync(id);

            if (session == null)
                return NotFound(ApiResponse<ExamSessionDetailsDto>.Fail("Exam session not found", 404));

            return Ok(ApiResponse<ExamSessionDetailsDto>.Ok(session, "Exam session details retrieved successfully"));
        }

        /// <summary>
        /// Deletes an exam session. Only allowed while the session is still a draft.
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var result = await _examSessionService.DeleteSessionAsync(id, actorId.Value);

            return result switch
            {
                DeleteExamSessionResult.NotFound => NotFound(ApiResponse<object>.Fail("Exam session not found", 404)),
                DeleteExamSessionResult.NotDraft => Conflict(ApiResponse<object>.Fail("Only draft sessions can be deleted", 409)),
                _ => Ok(ApiResponse<object>.Ok(null, "Exam session deleted successfully")),
            };
        }

        /// <summary>
        /// Updates all settings of an exam session. Only allowed while draft or SCHEDULED.
        /// LOCKED sessions can only be edited by SuperAdmin.
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> UpdateSession(int id, [FromBody] UpdateExamSessionRequest request)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var existingSession = await _examSessionService.GetSessionDetailsAsync(id);
            if (existingSession != null && existingSession.Status.ToString() == "LOCKED" && !IsSuperAdmin())
                return Forbid();

            var (result, session) = await _examSessionService.UpdateSessionAsync(id, request, actorId.Value);

            return result switch
            {
                UpdateExamSessionResult.NotFound => NotFound(ApiResponse<object>.Fail("Exam session not found", 404)),
                UpdateExamSessionResult.NotDraft => Conflict(ApiResponse<object>.Fail("Only draft sessions can be edited", 409)),
                UpdateExamSessionResult.QuestionBankNotFound => BadRequest(ApiResponse<object>.Fail("The selected question bank does not exist", 400)),
                UpdateExamSessionResult.InvalidSettings => BadRequest(ApiResponse<object>.Fail("Invalid session settings", 400)),
                _ => Ok(ApiResponse<ExamSessionDetailsDto>.Ok(session!, "Exam session updated successfully")),
            };
        }

        /// <summary>
        /// Edit restore: Updates proctors and students for a SCHEDULED session.
        /// </summary>
        [Authorize(Roles = "SuperAdmin")]
        [HttpPatch("{id:int}/edit-restore")]
        public async Task<IActionResult> EditRestoreSession(int id, [FromBody] EditRestoreSessionRequest request)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (result, session) = await _examSessionService.EditRestoreSessionAsync(id, request, actorId.Value);

            return result switch
            {
                EditRestoreSessionResult.NotFound => NotFound(ApiResponse<object>.Fail("Exam session not found", 404)),
                EditRestoreSessionResult.NotScheduled => Conflict(ApiResponse<object>.Fail("Only scheduled sessions can be edited with this endpoint", 409)),
                EditRestoreSessionResult.InvalidProctor => BadRequest(ApiResponse<object>.Fail("One or more assigned proctors are invalid or do not have Proctor role", 400)),
                EditRestoreSessionResult.ProctorNotAvailable => BadRequest(ApiResponse<object>.Fail("One or more assigned proctors have scheduling conflicts", 400)),
                EditRestoreSessionResult.InvalidStudent => BadRequest(ApiResponse<object>.Fail("One or more student IDs are invalid", 400)),
                _ => Ok(ApiResponse<ExamSessionDetailsDto>.Ok(session!, "Exam session updated successfully")),
            };
        }

        /// <summary>
        /// Publishes a draft exam session (DRAFT → SCHEDULED).
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("{id:int}/publish")]
        public async Task<IActionResult> PublishSession(int id)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (success, message) = await _transitionService.PublishSessionAsync(id, actorId.Value);

            if (!success)
                return BadRequest(ApiResponse<object>.Fail(message, 400));

            var session = await _examSessionService.GetSessionDetailsAsync(id);
            return Ok(ApiResponse<ExamSessionDetailsDto>.Ok(session, "Session published successfully", 200));
        }

        /// <summary>
        /// Extends an active exam session (ACTIVE → GRACE).
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin,Proctor")]
        [HttpPost("{id:int}/extend-time")]
        public async Task<IActionResult> ExtendSessionTime(int id, [FromBody] ExtendSessionTimeRequest request)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (success, message) = await _transitionService.ExtendSessionAsync(id, request.ExtraMinutes, actorId.Value);

            if (!success)
                return BadRequest(ApiResponse<object>.Fail(message, 400));

            var session = await _examSessionService.GetSessionDetailsAsync(id);
            return Ok(ApiResponse<ExamSessionDetailsDto>.Ok(session, "Session extended successfully", 200));
        }

        /// <summary>
        /// Returns available proctors for a given time slot (no scheduling conflicts).
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("available-proctors")]
        public async Task<IActionResult> GetAvailableProctors([FromQuery] DateTime startTime,[FromQuery] DateTime endTime)
        {
            if (startTime >= endTime)
                return BadRequest(ApiResponse<object>.Fail("Start time must be before end time", 400));

            var proctors = await _examSessionService.GetAvailableProctorsAsync(startTime, endTime);

            return Ok(ApiResponse<IEnumerable<AvailableProctorDto>>.Ok(
                proctors,"Available proctors retrieved successfully"));
        }

        /// <summary>
        /// Returns weekly exam session statistics.
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("weekly-statistics")]
        public async Task<IActionResult> GetWeeklyStatistics()
        {
            var result = await _examSessionService.GetWeeklyStatisticsAsync();

            return Ok(ApiResponse<IEnumerable<WeeklyExamSessionStatsDto>>.Ok(result, "Weekly statistics retrieved successfully"));
        }

        /// <summary>
        /// Exports grading report for a CLOSED or ARCHIVED exam session as CSV.
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("{id:int}/export/grading")]
        public async Task<IActionResult> ExportGradingReport(int id)
        {
            var csvContent = await _examSessionService.ExportGradingReportAsync(id);

            if (csvContent == null)
                return NotFound(ApiResponse<object>.Fail("Session not found", 404));

            if (csvContent == string.Empty)
                return Conflict(ApiResponse<object>.Fail("Session is not in exportable state (CLOSED or ARCHIVED)", 409));

            var filename = $"exam_session_{id}_grading_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "application/octet-stream", filename);
        }

        /// <summary>
        /// Exports audit log for a CLOSED or ARCHIVED exam session as CSV.
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("{id:int}/export/audit-log")]
        public async Task<IActionResult> ExportAuditLog(int id)
        {
            var csvContent = await _examSessionService.ExportAuditLogAsync(id);

            if (csvContent == null)
                return NotFound(ApiResponse<object>.Fail("Session not found", 404));

            if (csvContent == string.Empty)
                return Conflict(ApiResponse<object>.Fail("Session is not in exportable state (CLOSED or ARCHIVED)", 409));

            var filename = $"exam_session_{id}_audit_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "application/octet-stream", filename);
        }
    }
}