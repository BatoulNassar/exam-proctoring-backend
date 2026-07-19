using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using ExamProctoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
        }

        public async Task<RefreshToken> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens.Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.token == token);
        }

        public async Task RemoveByUserIdAsync(int userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.user_id == userId)
                .ToListAsync();

            foreach (var token in tokens)
            {
                _context.RefreshTokens.Remove(token);
            }
        }
    }
}
