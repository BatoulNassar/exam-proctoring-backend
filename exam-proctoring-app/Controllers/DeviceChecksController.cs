using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.DeviceChecks.DTOs;
using ExamProctoring.Application.Features.DeviceChecks.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    /// Pre-exam device-readiness telemetry from the Flutter Windows student client.
    /// The reported measurements are client-supplied and are never verified by the backend;
    /// this endpoint only records them for diagnostics and future proctor visibility.
    [ApiController]
    [Route("api/v1/device-checks")]
    [Authorize(Policy = AuthorizationPolicies.StudentOnly)]
    public class DeviceChecksController : ControllerBase
    {
        private const int MaxRequestBytes = 16 * 1024;

        private readonly IDeviceCheckService _deviceCheckService;
        private readonly IValidator<DeviceCheckRequest> _requestValidator;
        private readonly ILogger<DeviceChecksController> _logger;

        public DeviceChecksController(
            IDeviceCheckService deviceCheckService,
            IValidator<DeviceCheckRequest> requestValidator,
            ILogger<DeviceChecksController> logger)
        {
            _deviceCheckService = deviceCheckService;
            _requestValidator = requestValidator;
            _logger = logger;
        }

        /// Fire-and-forget from the client's perspective: success returns 202 with no body.
        [HttpPost]
        [RequestSizeLimit(MaxRequestBytes)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> Submit([FromBody] DeviceCheckRequest request)
        {
            try
            {
                // Validated explicitly: this project registers FluentValidation validators but does not
                // enable automatic MVC validation, so relying on the pipeline would silently skip the rules.
                var validation = await _requestValidator.ValidateAsync(request);
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

                // The StudentOnly policy already guarantees a positive integer student_id;
                // this re-read is defensive only.
                if (!int.TryParse(User.FindFirst("student_id")?.Value, out var studentId) || studentId <= 0)
                    return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(
                        "Student account is inactive.",
                        StatusCodes.Status403Forbidden,
                        AuthErrorCodes.AccountInactive));

                var deviceIdClaim = User.FindFirst("device_id")?.Value;

                var result = await _deviceCheckService.RecordAsync(request, studentId, deviceIdClaim);

                return result switch
                {
                    DeviceCheckResult.Accepted => Accepted(),

                    DeviceCheckResult.AccountInactive => StatusCode(
                        StatusCodes.Status403Forbidden,
                        ApiResponse<object>.Fail(
                            "Student account is inactive.",
                            StatusCodes.Status403Forbidden,
                            AuthErrorCodes.AccountInactive)),

                    DeviceCheckResult.DeviceMismatch => StatusCode(
                        StatusCodes.Status403Forbidden,
                        ApiResponse<object>.Fail(
                            "The device does not match the authenticated session.",
                            StatusCodes.Status403Forbidden,
                            AuthErrorCodes.DeviceMismatch)),

                    _ => NotFound(ApiResponse<object>.Fail(
                        "Exam session was not found.",
                        StatusCodes.Status404NotFound,
                        AuthErrorCodes.SessionNotFound)),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while recording a device check.");
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred while recording the device check", 500));
            }
        }

        private static string ToCamelCase(string propertyName) =>
            string.IsNullOrEmpty(propertyName)
                ? propertyName
                : char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }
}
