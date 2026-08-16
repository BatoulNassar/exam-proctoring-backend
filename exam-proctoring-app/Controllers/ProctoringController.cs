using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Streaming.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    /// ICE configuration for on-demand WebRTC (students and dashboard proctors).
    [ApiController]
    [Route("api/v1/proctoring")]
    [Authorize]
    public sealed class ProctoringController : ControllerBase
    {
        private readonly IIceServersService _iceServers;

        public ProctoringController(IIceServersService iceServers)
        {
            _iceServers = iceServers;
        }

        [HttpGet("ice-servers")]
        public IActionResult GetIceServers()
        {
            var data = _iceServers.GetIceServers();
            if (data == null)
            {
                return ApiResults.Fail(
                    StatusCodes.Status503ServiceUnavailable,
                    "ICE_CONFIG_UNAVAILABLE",
                    "ICE servers are not configured on this host.");
            }

            return ApiResults.Ok(data, "ICE servers retrieved.");
        }
    }
}
