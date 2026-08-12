using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Interfaces;
using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Roles.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRoleRepository _permissionRoleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(
            IRoleRepository roleRepository,
            IPermissionRoleRepository permissionRoleRepository,
            IPermissionRepository permissionRepository,
            IUnitOfWork unitOfWork)
        {
            _roleRepository = roleRepository;
            _permissionRoleRepository = permissionRoleRepository;
            _permissionRepository = permissionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
        {
            var permissions = await _permissionRepository.GetAllAsync();

            return permissions.Select(p => new PermissionDto
            {
                Id = p.id,
                Name = p.name,
                Description = p.description ?? string.Empty
            });
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesWithPermissionsAsync()
        {
            var roles = await _roleRepository.GetAllWithPermissionsAsync();

            return roles.Select(r => new RoleDto
            {
                Id = r.id,
                Name = r.name,
                Permissions = r.PermissionRoles.Select(pr => pr.Permission.name).ToList()
            });
        }

        public async Task<RoleDto> GetRolePermissionsAsync(int roleId)
        {
            var role = await _roleRepository.GetByIdWithPermissionsAsync(roleId);

            if (role == null)
                throw new InvalidOperationException("Role not found");

            return new RoleDto
            {
                Id = role.id,
                Name = role.name,
                Permissions = role.PermissionRoles.Select(pr => pr.Permission.name).ToList()
            };
        }

        public async Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds)
        {
            await _permissionRoleRepository.RemoveByRoleIdAsync(roleId);

            foreach (var permId in permissionIds)
            {
                await _permissionRoleRepository.AddAsync(new PermissionRole
                {
                    role_id = roleId,
                    permission_id = permId,
                    created_at = System.DateTime.UtcNow
                });
            }
            await _unitOfWork.SaveChangesAsync();
        }
    }
}