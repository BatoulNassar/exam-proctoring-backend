using ExamProctoring.API.Common;
using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Features.Users.DTOs;
using ExamProctoring.Application.Features.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Route("api/admins")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        private int? GetActorId()
        {
            var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(actorIdClaim, out var actorId) ? actorId : null;
        }


        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("with-permissions")]
        public async Task<IActionResult> GetAdminsWithPermissions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _userService.GetAllAdminsWithPermissionsAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<UserResponseDto>>.Ok(result, $"Retrieved {result.TotalCount} admins"));
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (success, message) = await _userService.DeleteUserAsync(userId, actorId.Value);

            if (!success)
                return BadRequest(ApiResponse<object>.Fail(message, 400));

            return Ok(ApiResponse<object>.Ok(null, message));
        }


        /// SuperAdmin creates a new admin account with temporary password
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequestDto request)
        {
            try
            {
                var result = await _userService.CreateAdminAsync(request);
                return Ok(ApiResponse<CreateAdminResponseDto>.Ok(result, "Admin account created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while creating admin.");
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred while creating admin", 500));
            }
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("{userId}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (success, message) = await _userService.DeactivateUserAsync(userId, actorId.Value);

            if (!success)
                return BadRequest(ApiResponse<object>.Fail(message, 400));

            return Ok(ApiResponse<object>.Ok(null, message));
        }


        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("{userId}/reactivate")]
        public async Task<IActionResult> ReactivateUser(int userId)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (success, message) = await _userService.ReactivateUserAsync(userId, actorId.Value);

            if (!success)
                return BadRequest(ApiResponse<object>.Fail(message, 400));

            return Ok(ApiResponse<object>.Ok(null, message));
        }
    }
}