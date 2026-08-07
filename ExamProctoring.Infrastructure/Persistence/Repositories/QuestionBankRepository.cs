using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class QuestionBankRepository : IQuestionBankRepository
    {
        private readonly AppDbContext _context;

        public QuestionBankRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync()
        {
            return await _context.QuestionBanks.CountAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.QuestionBanks.AnyAsync(qb => qb.id == id);
        }

        public async Task<QuestionBank> GetByIdAsync(int id)
        {
            return await _context.QuestionBanks
                .Include(qb => qb.Questions)
                .FirstOrDefaultAsync(qb => qb.id == id);
        }

        public async Task<IEnumerable<QuestionBank>> GetAllAsync()
        {
            return await _context.QuestionBanks
                .Include(qb => qb.Questions)
                .ToListAsync();
        }

        public async Task<QuestionBank?> GetActiveByCourseCodeAsync(string courseCode)
        {
            return await _context.QuestionBanks
                .FirstOrDefaultAsync(qb =>
                    qb.course_code == courseCode &&
                    qb.status != QuestionBankStatus.Archived);
        }

        public async Task<IEnumerable<QuestionBank>> GetDraftBanksWithSessionsAsync()
        {
            return await _context.QuestionBanks
                .Include(qb => qb.ExamSessions)
                .Where(qb => qb.status == QuestionBankStatus.Draft)
                .ToListAsync();
        }

        public async Task<IEnumerable<QuestionBank>> GetLockedBanksWithSessionsAsync()
        {
            return await _context.QuestionBanks
                .Include(qb => qb.ExamSessions)
                .Where(qb => qb.status == QuestionBankStatus.Locked)
                .ToListAsync();
        }

        public async Task AddAsync(QuestionBank questionBank)
        {
            await _context.QuestionBanks.AddAsync(questionBank);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(QuestionBank questionBank)
        {
            _context.QuestionBanks.Update(questionBank);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(QuestionBank questionBank)
        {
            _context.QuestionBanks.Remove(questionBank);
            await _context.SaveChangesAsync();
        }
    }
}
