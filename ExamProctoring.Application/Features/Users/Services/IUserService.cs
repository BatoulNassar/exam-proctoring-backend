using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Features.Users.DTOs;
using ExamProctoring.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ExamProctoring.Application.Features.Users.Services
{
    public interface IUserService
    {
        Task<CreateAdminResponseDto> CreateAdminAsync(CreateAdminRequestDto request);
        Task<PagedResult<UserResponseDto>> GetAllAdminsWithPermissionsAsync(int page, int pageSize);
        Task<(bool Success, string Message)> DeleteUserAsync(int userId, int actorId);
        Task<(bool Success, string Message)> DeactivateUserAsync(int userId, int actorId);
        Task<(bool Success, string Message)> ReactivateUserAsync(int userId, int actorId);
    }
}