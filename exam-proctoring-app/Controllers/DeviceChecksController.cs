using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.DeviceChecks.DTOs;
using ExamProctoring.Application.Features.DeviceChecks.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    /// Pre-exam device-readiness telemetry from the Flutter Windows student client.
    /// The reported measurements are client-supplied and are never verified by the backend;
    /// this endpoint only records them for diagnostics and future proctor visibility.
    ///
    /// Device matching and session visibility are decided by DeviceCheckService.
    [ApiController]
    [Route("api/v1/device-checks")]
    [Authorize(Policy = AuthorizationPolicies.StudentOnly)]
    [ValidateRequest]
    [ApiExceptionFilter(Message = "An error occurred while recording the device check")]
    public class DeviceChecksController : ControllerBase
    {
        private const int MaxRequestBytes = 16 * 1024;

        private readonly IDeviceCheckService _deviceCheckService;

        public DeviceChecksController(IDeviceCheckService deviceCheckService)
        {
            _deviceCheckService = deviceCheckService;
        }

        /// Fire-and-forget from the client's perspective: success returns 202 with no body.
        [HttpPost]
        [RequestSizeLimit(MaxRequestBytes)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> Submit([FromBody] DeviceCheckRequest request)
        {
            if (!User.TryGetStudentId(out var studentId))
                return ApiResults.AccountInactive();

            var result = await _deviceCheckService.RecordAsync(request, studentId, User.GetDeviceId());

            return StudentFlowResultMapper.MapDeviceCheck(result);
        }
    }
}
