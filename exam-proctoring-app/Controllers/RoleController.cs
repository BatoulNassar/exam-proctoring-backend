using ExamProctoring.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize(Roles = "SuperAdmin")] 
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var result = await _roleService.GetAllRolesWithPermissionsAsync();
            return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(result, "Roles with permissions retrieved successfully"));
        }


        [HttpPut("{roleId}/permissions")]
        public async Task<IActionResult> UpdatePermissions(int roleId, [FromBody] UpdateRolePermissionsRequest request)
        {
            await _roleService.UpdateRolePermissionsAsync(roleId, request.PermissionIds);

            var updatedRoles = await _roleService.GetAllRolesWithPermissionsAsync();

            return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(updatedRoles, "Permissions updated successfully"));
        }
    }
}