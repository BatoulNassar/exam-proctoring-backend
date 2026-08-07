using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Common.Settings;
using ExamProctoring.Application.Features.Auth.DTOs;
using ExamProctoring.Domain.Entities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using ExamProctoring.Application.Common.Interfaces;

namespace ExamProctoring.Application.Features.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;

        public AuthService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository,IPasswordHasher passwordHasher,IJwtTokenGenerator jwtTokenGenerator,IUnitOfWork unitOfWork,IRoleRepository roleRepository,IPermissionRepository permissionRepository,IUserRoleRepository userRoleRepository,IOptions<JwtSettings> jwtSettings, IEmailService emailService)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _unitOfWork = unitOfWork;
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _userRoleRepository = userRoleRepository;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByEmailWithRolesAndPermissionsAsync(request.Email);

            if (user == null || !_passwordHasher.Verify(request.Password, user.password_hash))
            {
                throw new AuthenticationException("Invalid email or password");
            }

            var roles = user.UserRoles.Select(ur => ur.Role.name).Distinct().ToList();
            var permissions = user.UserRoles
                 .SelectMany(ur => ur.Role.PermissionRoles)
                    .Select(pr => pr.Permission.name)
                     .Distinct()
              .ToList();

            return await GenerateTokensForUserAsync(user, roles, permissions);
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (existingToken == null)
            {
                throw new AuthenticationException("Invalid refresh token");
            }

            if (existingToken.revoked_at != null)
            {
                throw new AuthenticationException("Refresh token has been revoked, please login again");
            }

            if (existingToken.expires_at < DateTime.UtcNow)
            {
                throw new AuthenticationException("Refresh token has expired, please login again");
            }

            var user = await _userRepository.GetByEmailWithRolesAndPermissionsAsync(existingToken.User.email);

            var roles = user.UserRoles.Select(ur => ur.Role.name).Distinct().ToList();
            var permissions = user.UserRoles
          .SelectMany(ur => ur.Role.PermissionRoles)
        .Select(pr => pr.Permission.name)
          .Distinct()
                     .ToList();

            var response = await GenerateTokensForUserAsync(user, roles, permissions);

            existingToken.revoked_at = DateTime.UtcNow;
            existingToken.replaced_by_token = response.RefreshToken;

            await _unitOfWork.SaveChangesAsync();

            return response;
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (existingToken == null || existingToken.revoked_at != null)
            {
                return;
            }

            existingToken.revoked_at = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<LoginResponseDto> GenerateTokensForUserAsync( User user, List<string> roles, List<string> permissions)
        {
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles, permissions);
            var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            var refreshTokenEntity = new RefreshToken
            {
                user_id = user.id,
                token = refreshTokenValue,
                expires_at = refreshTokenExpiresAt,
                created_at = DateTime.UtcNow
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshToken = refreshTokenValue,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                UserId = user.id,
                UserName = user.user_name,
                FullName = user.full_name,
                Email = user.email,
                Roles = roles,
                Permissions = permissions
            };
        }

        public async Task<LoginResponseDto> ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                throw new InvalidOperationException("Current password is required");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                throw new InvalidOperationException("New password is required");

            if (request.NewPassword != request.ConfirmPassword)
                throw new InvalidOperationException("Passwords do not match");

            if (request.NewPassword.Length < 8)
                throw new InvalidOperationException("Password must be at least 8 characters");

            var user = await _userRepository.GetByIdWithRolesAndPermissionsAsync(userId);

            if (user == null)
                throw new InvalidOperationException("User not found");

            if (!_passwordHasher.Verify(request.CurrentPassword, user.password_hash))
                throw new AuthenticationException("Current password is incorrect");

            user.password_hash = _passwordHasher.Hash(request.NewPassword);
            user.updated_at = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.name).Distinct().ToList();
            var permissions = user.UserRoles.SelectMany(ur => ur.Role.PermissionRoles)
                                           .Select(pr => pr.Permission.name).Distinct().ToList();


            return await GenerateTokensForUserAsync(user, roles, permissions);
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
                throw new InvalidOperationException("No account found with this email address");

            var otp = new Random().Next(100000, 999999).ToString();
            user.reset_otp = otp;
            user.reset_otp_expires_at = DateTime.UtcNow.AddMinutes(15);
            await _unitOfWork.SaveChangesAsync();

            var body = $@"
                <div style='font-family:Arial,sans-serif;max-width:400px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:8px'>
                    <h2 style='color:#1d4ed8'>إعادة تعيين كلمة المرور</h2>
                    <p>رمز التحقق الخاص بك:</p>
                    <div style='font-size:32px;font-weight:bold;letter-spacing:8px;color:#1d4ed8;text-align:center;padding:16px;background:#eff6ff;border-radius:4px'>
                        {otp}
                    </div>
                    <p style='color:#6b7280;font-size:13px;margin-top:16px'>صالح لمدة 15 دقيقة. لا تشاركه مع أحد.</p>
                </div>";

            await _emailService.SendAsync(email, "رمز إعادة تعيين كلمة المرور", body);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
                throw new InvalidOperationException("Password must be at least 8 characters");

            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null || user.reset_otp == null || user.reset_otp_expires_at == null)
                throw new InvalidOperationException("Invalid or expired OTP");

            if (user.reset_otp != request.Otp)
                throw new InvalidOperationException("Invalid OTP");

            if (user.reset_otp_expires_at < DateTime.UtcNow)
                throw new InvalidOperationException("OTP has expired");

            user.password_hash = _passwordHasher.Hash(request.NewPassword);
            user.reset_otp = null;
            user.reset_otp_expires_at = null;
            user.updated_at = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
