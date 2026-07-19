using ExamProctoring.Domain.Entities;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken refreshToken);
        Task<RefreshToken> GetByTokenAsync(string token);
        Task RemoveByUserIdAsync(int userId);
    }
}
