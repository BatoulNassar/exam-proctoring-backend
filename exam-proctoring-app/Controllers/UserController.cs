using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Users.DTOs;
using ExamProctoring.Application.Features.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExamProctoring.Application.Features.Users.DTOs;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Route("api/admins")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }


        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("with-permissions")]
        public async Task<IActionResult> GetAdminsWithPermissions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _userService.GetAllAdminsWithPermissionsAsync(page, pageSize);
            return Ok(ApiResponse<IEnumerable<UserResponseDto>>.Ok(result, "Admins with permissions retrieved successfully"));
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{adminId}")]
        public async Task<IActionResult> DeleteAdmin(int adminId)
        {
            await _userService.DeleteAdminAsync(adminId);
            return Ok(ApiResponse<object>.Ok(null!, "Admin deleted successfully"));
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
                return StatusCode(500, ApiResponse<object>.Fail("An error occurred while creating admin", 500));
            }
        }
    }
}