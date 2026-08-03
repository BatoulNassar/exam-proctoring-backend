using ExamProctoring.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IStudentLoginAttemptRepository
    {
        Task<StudentLoginAttempt?> GetByIdentifierHashAsync(string identifierHash);

        /// Atomically increments the counter for an unresolved identifier, creating the row on first use,
        /// and stamps the lockout window when <paramref name="maxAttempts"/> is reached.
        /// Returns the persisted state after the update.
        Task<(int FailedAttempts, DateTime? LockoutEndUtc)> RegisterFailedAttemptAsync(
            string identifierHash, int maxAttempts, DateTime lockoutEndUtc, DateTime nowUtc);

        Task ResetAsync(string identifierHash, DateTime nowUtc);
    }
}
