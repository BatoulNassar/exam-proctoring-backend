using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class SystemSettingsRepository : ISystemSettingsRepository
    {
        private readonly AppDbContext _context;

        public SystemSettingsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SystemSettings?> GetAsync()
        {
            return await _context.SystemSettings.FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(SystemSettings settings)
        {
            _context.SystemSettings.Update(settings);
        }

        public async Task AddAsync(SystemSettings settings)
        {
            await _context.SystemSettings.AddAsync(settings);
        }
    }
}
