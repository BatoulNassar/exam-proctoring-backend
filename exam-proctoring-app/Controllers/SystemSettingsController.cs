using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Settings.DTOs;
using ExamProctoring.Application.Features.Settings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Route("api/settings")]
    [Authorize(Roles = "SuperAdmin")]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ISystemSettingsService _service;

        public SystemSettingsController(ISystemSettingsService service)
        {
            _service = service;
        }

        private int? GetActorId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }


        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _service.GetSettingsAsync();
            if (settings == null)
                return NotFound(ApiResponse<object>.Fail("System settings not found", 404));

            return Ok(ApiResponse<SystemSettingsDto>.Ok(settings, "Settings retrieved successfully"));
        }


        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] SystemSettingsDto dto)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (success, message, updatedData) = await _service.UpdateSettingsAsync(dto, actorId.Value);

            return success
                ? Ok(ApiResponse<SystemSettingsDto>.Ok(updatedData, message))
                : BadRequest(ApiResponse<object>.Fail(message, 400));
        }

    }
}
