using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class ProctorActionRepository : IProctorActionRepository
    {
        private readonly AppDbContext _context;

        public ProctorActionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ProctorAction action)
        {
            await _context.ProctorActions.AddAsync(action);
        }

        public async Task<int> CountWarningsForStudentSessionAsync(int studentSessionId)
        {
            return await _context.ProctorActions
                .CountAsync(pa => pa.action_type == ProctorActionType.Warn
                               && pa.AlertEvent.student_session_id == studentSessionId);
        }
    }
}
