using ExamProctoring.Domain.Entities;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IStudentRepository
    {
        Task<IEnumerable<Student>> GetAllAsync();
        Task<IEnumerable<Student>> GetPagedAsync(int page, int pageSize);
        Task<int> CountAsync();
        Task<List<Student>> GetByUniversityNumbersAsync(IEnumerable<string> universityNumbers);
        Task<List<Student>> GetByIdsAsync(int[] studentIds);
        Task<Student?> GetByUniversityNumberAsync(string universityNumber);
        Task<Student?> GetByIdAsync(int studentId);
        Task<Student?> GetByEmailAsync(string email);
        Task<Student?> GetByUserNameAsync(string userName);
        Task AddAsync(Student student);
        Task UpdateAsync(Student student);

        /// Atomically increments the failed-login counter and, on reaching <paramref name="maxAttempts"/>,
        /// stamps the lockout window — in a single UPDATE so concurrent failures cannot overwrite each other.
        /// Returns the persisted state after the update.
        Task<(int FailedAttempts, DateTime? LockoutEndUtc)> RegisterFailedLoginAsync(
            int studentId, int maxAttempts, DateTime lockoutEndUtc, DateTime nowUtc);

        /// Clears the failed-login counter and any lockout for the student.
        Task ResetLoginStateAsync(int studentId, DateTime nowUtc);
    }
}