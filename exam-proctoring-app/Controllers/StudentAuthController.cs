using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.StudentAuth.DTOs;
using ExamProctoring.Application.Features.StudentAuth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    /// Authentication for the Flutter Windows student desktop application.
    /// Separate from the dashboard AuthController: students live in the Student table,
    /// hold no dashboard roles or permissions, and receive no refresh token.
    ///
    /// Lockout counting, app-version rules and credential verification all live in
    /// StudentAuthService; this controller only binds, calls it, and maps the outcome.
    [ApiController]
    [Route("api/v1/auth")]
    [ValidateRequest]
    [ApiExceptionFilter(Message = "An error occurred during login")]
    public class StudentAuthController : ControllerBase
    {
        private readonly IStudentAuthService _studentAuthService;

        public StudentAuthController(IStudentAuthService studentAuthService)
        {
            _studentAuthService = studentAuthService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] StudentLoginRequest request)
        {
            var result = await _studentAuthService.LoginAsync(request);

            return StudentFlowResultMapper.MapLogin(result, Response);
        }
    }
}
