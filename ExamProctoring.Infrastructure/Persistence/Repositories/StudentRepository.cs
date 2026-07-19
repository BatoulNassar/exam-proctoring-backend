using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly AppDbContext _context;


        public StudentRepository(AppDbContext context)
        {
            _context = context;
        }


        public async Task<IEnumerable<Student>> GetAllAsync()
        {
            return await _context.Students
                .OrderBy(s => s.first_name)
                .ToListAsync();
        }
    }
}