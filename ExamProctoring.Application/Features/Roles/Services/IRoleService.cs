public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllRolesWithPermissionsAsync();
    Task<RoleDto> GetRolePermissionsAsync(int roleId);
    Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);
}