using ExamProctoring.Application.Features.Auth.DTOs;
using ExamProctoring.Domain.Entities;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Auth.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string refreshToken);
        Task<LoginResponseDto> GenerateTokensForUserAsync(User user, List<string> roles, List<string> permissions);
        Task<LoginResponseDto> ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(ResetPasswordRequestDto request);
    }

}
