using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.StudentAuth.DTOs;
using ExamProctoring.Application.Features.StudentAuth.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    /// Authentication for the Flutter Windows student desktop application.
    /// Separate from the dashboard AuthController: students live in the Student table,
    /// hold no dashboard roles or permissions, and receive no refresh token.
    [ApiController]
    [Route("api/v1/auth")]
    public class StudentAuthController : ControllerBase
    {
        private readonly IStudentAuthService _studentAuthService;
        private readonly IValidator<StudentLoginRequest> _loginValidator;

        public StudentAuthController(
            IStudentAuthService studentAuthService,
            IValidator<StudentLoginRequest> loginValidator)
        {
            _studentAuthService = studentAuthService;
            _loginValidator = loginValidator;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] StudentLoginRequest request)
        {
            try
            {
                // Validated explicitly: this project registers FluentValidation validators but does not
                // enable automatic MVC validation, so relying on the pipeline would silently skip the rules.
                var validation = await _loginValidator.ValidateAsync(request);
                if (!validation.IsValid)
                {
                    var errors = validation.Errors
                        .GroupBy(failure => ToCamelCase(failure.PropertyName))
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(failure => failure.ErrorMessage).ToArray());

                    return BadRequest(ApiResponse<object>.Fail(
                        "Validation failed.", 400, AuthErrorCodes.ValidationFailed, new { errors }));
                }

                var result = await _studentAuthService.LoginAsync(request);

                switch (result.Status)
                {
                    case StudentLoginStatus.Success:
                        return Ok(ApiResponse<StudentLoginResponse>.Ok(result.Response!, "Login successful."));

                    case StudentLoginStatus.AppVersionUnsupported:
                        return StatusCode(StatusCodes.Status426UpgradeRequired, ApiResponse<object>.Fail(
                            "Application update is required.",
                            StatusCodes.Status426UpgradeRequired,
                            AuthErrorCodes.AppVersionUnsupported,
                            new { minimumVersion = result.MinimumVersion }));

                    case StudentLoginStatus.AccountLocked:
                        Response.Headers["Retry-After"] = result.RetryAfterSeconds!.Value.ToString();
                        return StatusCode(StatusCodes.Status423Locked, ApiResponse<object>.Fail(
                            "Account is temporarily locked.",
                            StatusCodes.Status423Locked,
                            AuthErrorCodes.AccountLocked,
                            new { retryAfterSeconds = result.RetryAfterSeconds }));

                    case StudentLoginStatus.AccountInactive:
                        return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(
                            "Student account is inactive.",
                            StatusCodes.Status403Forbidden,
                            AuthErrorCodes.AccountInactive));

                    default:
                        return Unauthorized(ApiResponse<object>.Fail(
                            "Invalid identifier or password.",
                            StatusCodes.Status401Unauthorized,
                            AuthErrorCodes.InvalidCredentials,
                            new { remainingAttempts = result.RemainingAttempts }));
                }
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred during login", 500));
            }
        }

        private static string ToCamelCase(string propertyName) =>
            string.IsNullOrEmpty(propertyName)
                ? propertyName
                : char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }
}
