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

        public async Task<List<Student>> GetByUniversityNumbersAsync(IEnumerable<string> universityNumbers)
        {
            return await _context.Students
                .Where(s => universityNumbers.Contains(s.university_number))
                .ToListAsync();
        }

        public async Task<List<Student>> GetByIdsAsync(int[] studentIds)
        {
            return await _context.Students
                .Where(s => studentIds.Contains(s.id))
                .ToListAsync();
        }

        public async Task<Student?> GetByUniversityNumberAsync(string universityNumber)
        {
            return await _context.Students
                .FirstOrDefaultAsync(s => s.university_number == universityNumber);
        }

        public async Task<Student?> GetByIdAsync(int studentId)
        {
            return await _context.Students
                .FirstOrDefaultAsync(s => s.id == studentId);
        }

        public async Task AddAsync(Student student)
        {
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }
    }
}