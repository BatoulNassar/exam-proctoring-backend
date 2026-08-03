using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Auth.DTOs;
using ExamProctoring.Application.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Login successful"));
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ApiResponse<object>.Fail(ex.Message, 401));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during dashboard login.");
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred during login", 500));
            }
        }


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Token refreshed successfully"));
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ApiResponse<object>.Fail(ex.Message, 401));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during token refresh.");
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred while refreshing token", 500));
            }
        }


        [Authorize(Policy = AuthorizationPolicies.DashboardOnly)]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                await _authService.RevokeTokenAsync(request.RefreshToken);
                return Ok(ApiResponse<object>.Ok(null!, "Logged out successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during logout.");
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred during logout", 500));
            }
        }


        [Authorize(Policy = AuthorizationPolicies.DashboardOnly)]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail("Invalid user ID", 401));
                }

                var result = await _authService.ChangePasswordAsync(userId, request);
                return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Password changed successfully"));
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ApiResponse<object>.Fail(ex.Message, 401));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while changing password.");
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred while changing password", 500));
            }
        }

    }
}
