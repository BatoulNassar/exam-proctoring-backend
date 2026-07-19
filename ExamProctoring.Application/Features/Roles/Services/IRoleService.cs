public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllRolesWithPermissionsAsync();
    Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);
}