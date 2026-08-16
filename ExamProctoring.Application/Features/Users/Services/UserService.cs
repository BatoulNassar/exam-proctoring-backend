using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Auth.Services;
using ExamProctoring.Application.Features.Users.DTOs;
using ExamProctoring.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;



namespace ExamProctoring.Application.Features.Users.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public UserService(IUserRepository userRepository,IRoleRepository roleRepository, IUserRoleRepository userRoleRepository, IRefreshTokenRepository refreshTokenRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork, IAuthService authService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        public async Task<CreateAdminResponseDto> CreateAdminAsync(CreateAdminRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new InvalidOperationException("Full name is required");

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new InvalidOperationException("Email is required");

            if (request.RoleIds == null || !request.RoleIds.Any())
                throw new InvalidOperationException("At least one role must be assigned");

            if (await _userRepository.EmailExistsAsync(request.Email))
                throw new InvalidOperationException("Email already registered");

            var tempPassword = GenerateTemporaryPassword();
            var userName = request.Email.Split('@')[0];

            var adminUser = new User
            {
                user_name = userName,
                full_name = request.FullName,
                email = request.Email,
                password_hash = _passwordHasher.Hash(tempPassword),
                created_at = DateTime.UtcNow
            };

            await _userRepository.AddAsync(adminUser);
            await _unitOfWork.SaveChangesAsync();

            var rolesToAssign = await _roleRepository.GetByIdsAsync(request.RoleIds);

            if (rolesToAssign == null || rolesToAssign.Count() != request.RoleIds.Count)
                throw new InvalidOperationException("One or more roles are invalid");

            foreach (var role in rolesToAssign)
            {
                await _userRoleRepository.AddAsync(new User_Roles
                {
                    user_id = adminUser.id,
                    role_id = role.id,
                    created_at = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();

            var createdAdmin = await _userRepository.GetByIdWithRolesAndPermissionsAsync(adminUser.id);

            return new CreateAdminResponseDto
            {
                UserId = adminUser.id,
                UserName = adminUser.user_name,
                FullName = adminUser.full_name,
                Email = adminUser.email,
                TemporaryPassword = tempPassword,
                AssignedRoles = createdAdmin.UserRoles.Select(ur => ur.Role.name).ToList(),
            };
        }

        public async Task<(bool Success, string Message)> DeleteUserAsync(int userId, int actorId)
        {
            if (userId <= 0)
                return (false, "Invalid user ID");

            if (userId == actorId)
                return (false, "Cannot delete your own account");

            var user = await _userRepository.GetByIdWithRolesAndPermissionsAsync(userId);
            if (user == null || user.is_deleted)
                return (false, "User not found");

            var isSuperAdmin = user.UserRoles?.Any(ur => ur.Role?.name == "SuperAdmin") ?? false;
            if (isSuperAdmin)
                return (false, "Cannot delete a SuperAdmin account");

            await _refreshTokenRepository.RemoveByUserIdAsync(userId);

            user.is_deleted = true;
            user.is_active = false;
            user.deleted_at = DateTime.UtcNow;
            user.deleted_by = actorId;
            user.updated_at = DateTime.UtcNow;
            user.updated_by = actorId;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return (true, "User deleted successfully");
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Range(0, 12)
           .Select(_ => chars[random.Next(chars.Length)])
           .ToArray());
        }

        public async Task<PagedResult<UserResponseDto>> GetAllAdminsWithPermissionsAsync(int page, int pageSize)
        {
            var admins = await _userRepository.GetAdminsWithPermissionsPagedAsync(page, pageSize);
            var totalCount = await _userRepository.CountAdminsWithPermissionsAsync();

            var items = admins.Select(a => new UserResponseDto
            {
                Id = a.id,
                UserName = a.user_name ?? string.Empty,
                Permissions = a.UserRoles?.SelectMany(ur => ur.Role.PermissionRoles).Select(pr => pr.Permission.name).Distinct().ToList() ?? new List<string>()
            }).ToList();

            return new PagedResult<UserResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<(bool Success, string Message)> DeactivateUserAsync(int userId, int actorId)
        {
            if (userId == actorId)
                return (false, "Cannot deactivate your own account");

            var user = await _userRepository.GetByIdWithRolesAndPermissionsAsync(userId);
            if (user == null || user.is_deleted)
                return (false, "User not found");

            var isSuperAdmin = user.UserRoles?.Any(ur => ur.Role.name == "SuperAdmin") ?? false;
            if (isSuperAdmin)
                return (false, "Cannot deactivate a SuperAdmin account");

            if (!user.is_active)
                return (false, "Account is already deactivated");

            user.is_active = false;
            user.updated_at = DateTime.UtcNow;
            user.updated_by = actorId;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return (true, "Account deactivated successfully");
        }

        public async Task<(bool Success, string Message)> ReactivateUserAsync(int userId, int actorId)
        {
            var user = await _userRepository.GetByIdWithRolesAndPermissionsAsync(userId);
            if (user == null || user.is_deleted)
                return (false, "User not found");

            var isSuperAdmin = user.UserRoles?.Any(ur => ur.Role.name == "SuperAdmin") ?? false;
            if (isSuperAdmin)
                return (false, "Cannot modify a SuperAdmin account");

            if (user.is_active)
                return (false, "Account is already active");

            user.is_active = true;
            user.updated_at = DateTime.UtcNow;
            user.updated_by = actorId;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return (true, "Account reactivated successfully");
        }
    }
}