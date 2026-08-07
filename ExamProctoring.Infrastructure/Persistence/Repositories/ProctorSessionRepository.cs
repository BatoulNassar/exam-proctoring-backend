using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class ProctorSessionRepository : IProctorSessionRepository
    {
        private readonly AppDbContext _context;

        public ProctorSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProctorSession>> GetByExamSessionIdAsync(int examSessionId)
        {
            return await _context.ProctorSessions
                .Where(ps => ps.exam_session_id == examSessionId)
                .Include(ps => ps.Proctor)
                .ToListAsync();
        }

        public async Task<ProctorSession?> GetByExamSessionAndProctorAsync(int examSessionId, int proctorId)
        {
            return await _context.ProctorSessions
                .SingleOrDefaultAsync(ps => ps.exam_session_id == examSessionId && ps.proctor_id == proctorId);
        }

        public async Task<IReadOnlyList<int>> GetSessionIdsByProctorAsync(int proctorId)
        {
            return await _context.ProctorSessions
                .Where(ps => ps.proctor_id == proctorId)
                .Select(ps => ps.exam_session_id)
                .Distinct()
                .ToListAsync();
        }

        public async Task AddAsync(ProctorSession proctorSession)
        {
            await _context.ProctorSessions.AddAsync(proctorSession);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(ProctorSession proctorSession)
        {
            _context.ProctorSessions.Remove(proctorSession);
            await _context.SaveChangesAsync();
        }
    }
}
