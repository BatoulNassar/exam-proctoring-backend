public interface IRoleService
{
    /// <summary>
    /// Every permission the system defines, with its id. The roles screen needs
    /// this to render the full list and to submit ids on save.
    /// </summary>
    Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync();

    Task<IEnumerable<RoleDto>> GetAllRolesWithPermissionsAsync();
    Task<RoleDto> GetRolePermissionsAsync(int roleId);
    Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);
}