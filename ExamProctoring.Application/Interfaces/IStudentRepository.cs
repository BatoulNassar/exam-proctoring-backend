using ExamProctoring.Domain.Entities;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IStudentRepository
    {
        Task<IEnumerable<Student>> GetAllAsync();
        Task<List<Student>> GetByUniversityNumbersAsync(IEnumerable<string> universityNumbers);
        Task<List<Student>> GetByIdsAsync(int[] studentIds);
        Task<Student?> GetByUniversityNumberAsync(string universityNumber);
        Task<Student?> GetByIdAsync(int studentId);
        Task AddAsync(Student student);
        Task UpdateAsync(Student student);
    }
}