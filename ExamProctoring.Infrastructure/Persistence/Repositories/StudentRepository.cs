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

        public async Task<IEnumerable<Student>> GetPagedAsync(int page, int pageSize)
        {
            return await _context.Students
                // Ordered by id as the tiebreaker: first_name alone is not unique, and
                // an unstable sort makes rows jump between pages.
                .OrderBy(s => s.first_name)
                .ThenBy(s => s.id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.Students.CountAsync();
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

        public async Task<Student?> GetByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            return await _context.Students
                .FirstOrDefaultAsync(s => s.email.ToLower() == normalizedEmail);
        }

        public async Task<Student?> GetByUserNameAsync(string userName)
        {
            var normalizedUserName = userName.Trim().ToLower();

            return await _context.Students
                .FirstOrDefaultAsync(s => s.user_name.ToLower() == normalizedUserName);
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

        public async Task<(int FailedAttempts, DateTime? LockoutEndUtc)> RegisterFailedLoginAsync(
            int studentId, int maxAttempts, DateTime lockoutEndUtc, DateTime nowUtc)
        {
            // One server-side UPDATE: the increment and the lockout decision are evaluated by SQL Server
            // against the current row, so two concurrent failures cannot read-then-overwrite the same count.
            await _context.Students
                .Where(s => s.id == studentId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.failed_login_attempts, s => s.failed_login_attempts + 1)
                    .SetProperty(s => s.lockout_end_utc,
                        s => s.failed_login_attempts + 1 >= maxAttempts ? lockoutEndUtc : s.lockout_end_utc)
                    .SetProperty(s => s.updated_at, nowUtc));

            var state = await _context.Students
                .AsNoTracking()
                .Where(s => s.id == studentId)
                .Select(s => new { s.failed_login_attempts, s.lockout_end_utc })
                .FirstOrDefaultAsync();

            return state == null
                ? (0, null)
                : (state.failed_login_attempts, state.lockout_end_utc);
        }

        public async Task ResetLoginStateAsync(int studentId, DateTime nowUtc)
        {
            await _context.Students
                .Where(s => s.id == studentId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.failed_login_attempts, 0)
                    .SetProperty(s => s.lockout_end_utc, (DateTime?)null)
                    .SetProperty(s => s.updated_at, nowUtc));
        }
    }
}