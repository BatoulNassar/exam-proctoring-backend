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
        Task DeleteAdminAsync(int adminId);
        Task<IEnumerable<UserResponseDto>> GetAllAdminsWithPermissionsAsync(int page, int pageSize);
        Task<(bool Success, string Message)> DeactivateAdminAsync(int adminId, int actorId);
        Task<(bool Success, string Message)> ReactivateAdminAsync(int adminId, int actorId);
    }
}

