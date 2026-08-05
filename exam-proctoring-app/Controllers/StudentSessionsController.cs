using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Eligibility.DTOs;
using ExamProctoring.Application.Features.Eligibility.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    /// Exam-session queries for the Flutter Windows student desktop application.
    /// Student tokens only; dashboard tokens are rejected by the StudentOnly policy.
    [ApiController]
    [Route("api/v1/sessions")]
    [Authorize(Policy = AuthorizationPolicies.StudentOnly)]
    public class StudentSessionsController : ControllerBase
    {
        private readonly IEligibilityService _eligibilityService;
        private readonly ILogger<StudentSessionsController> _logger;

        public StudentSessionsController(
            IEligibilityService eligibilityService,
            ILogger<StudentSessionsController> logger)
        {
            _eligibilityService = eligibilityService;
            _logger = logger;
        }

        /// May the authenticated student start an exam right now? Read-only; takes no
        /// route parameter, query parameter or body - the student is identified by the token.
        [HttpGet("eligibility")]
        public async Task<IActionResult> GetEligibility()
        {
            try
            {
                // The StudentOnly policy already guarantees a positive integer student_id;
                // this re-read is defensive only.
                if (!int.TryParse(User.FindFirst("student_id")?.Value, out var studentId) || studentId <= 0)
                    return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(
                        "Student account is inactive.",
                        StatusCodes.Status403Forbidden,
                        AuthErrorCodes.AccountInactive));

                var result = await _eligibilityService.GetEligibilityAsync(studentId);

                return result.Status switch
                {
                    EligibilityStatus.AccountInactive => StatusCode(
                        StatusCodes.Status403Forbidden,
                        ApiResponse<object>.Fail(
                            "Student account is inactive.",
                            StatusCodes.Status403Forbidden,
                            AuthErrorCodes.AccountInactive)),

                    EligibilityStatus.MultipleActiveSessions => Conflict(
                        ApiResponse<object>.Fail(
                            "Multiple active exam sessions are assigned to the student.",
                            StatusCodes.Status409Conflict,
                            AuthErrorCodes.MultipleActiveSessions)),

                    _ => Ok(ApiResponse<EligibilityResponse>.Ok(
                        result.Response!, "Eligibility checked successfully.")),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while checking exam eligibility.");
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred while checking eligibility", 500));
            }
        }
    }
}
