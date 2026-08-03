using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class StudentLoginAttemptRepository : IStudentLoginAttemptRepository
    {
        private readonly AppDbContext _context;

        public StudentLoginAttemptRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StudentLoginAttempt?> GetByIdentifierHashAsync(string identifierHash)
        {
            return await _context.StudentLoginAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(la => la.identifier_hash == identifierHash);
        }

        public async Task<(int FailedAttempts, DateTime? LockoutEndUtc)> RegisterFailedAttemptAsync(
            string identifierHash, int maxAttempts, DateTime lockoutEndUtc, DateTime nowUtc)
        {
            // Same server-side pattern as the Student counter: the increment and the lockout decision are
            // evaluated by SQL Server against the current row, so concurrent failures cannot overwrite
            // each other. The row is created on first use; a concurrent insert loses the unique index
            // race and is retried as an update.
            for (var pass = 0; pass < 2; pass++)
            {
                var updated = await _context.StudentLoginAttempts
                    .Where(la => la.identifier_hash == identifierHash)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(la => la.failed_attempts, la => la.failed_attempts + 1)
                        .SetProperty(la => la.lockout_end_utc,
                            la => la.failed_attempts + 1 >= maxAttempts ? lockoutEndUtc : la.lockout_end_utc)
                        .SetProperty(la => la.updated_at, nowUtc));

                if (updated > 0)
                    break;

                try
                {
                    await _context.StudentLoginAttempts.AddAsync(new StudentLoginAttempt
                    {
                        identifier_hash = identifierHash,
                        failed_attempts = 1,
                        lockout_end_utc = 1 >= maxAttempts ? lockoutEndUtc : null,
                        created_at = nowUtc,
                    });

                    await _context.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException)
                {
                    // Another request inserted the same identifier first; fall through and update it.
                    _context.ChangeTracker.Clear();
                }
            }

            var state = await _context.StudentLoginAttempts
                .AsNoTracking()
                .Where(la => la.identifier_hash == identifierHash)
                .Select(la => new { la.failed_attempts, la.lockout_end_utc })
                .FirstOrDefaultAsync();

            return state == null
                ? (0, null)
                : (state.failed_attempts, state.lockout_end_utc);
        }

        public async Task ResetAsync(string identifierHash, DateTime nowUtc)
        {
            await _context.StudentLoginAttempts
                .Where(la => la.identifier_hash == identifierHash)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(la => la.failed_attempts, 0)
                    .SetProperty(la => la.lockout_end_utc, (DateTime?)null)
                    .SetProperty(la => la.updated_at, nowUtc));
        }
    }
}
