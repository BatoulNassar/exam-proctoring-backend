using ExamProctoring.API.Common;
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
    [ApiExceptionFilter(Message = "An error occurred while checking eligibility")]
    public class StudentSessionsController : ControllerBase
    {
        private readonly IEligibilityService _eligibilityService;

        public StudentSessionsController(IEligibilityService eligibilityService)
        {
            _eligibilityService = eligibilityService;
        }

        /// May the authenticated student start an exam right now? Read-only; takes no route
        /// parameter, query parameter or body - the student is identified by the token.
        /// Which session applies and whether it is startable is decided by EligibilityService.
        [HttpGet("eligibility")]
        public async Task<IActionResult> GetEligibility()
        {
            if (!User.TryGetStudentId(out var studentId))
                return ApiResults.AccountInactive();

            var result = await _eligibilityService.GetEligibilityAsync(studentId);

            return StudentFlowResultMapper.MapEligibility(result);
        }
    }
}
