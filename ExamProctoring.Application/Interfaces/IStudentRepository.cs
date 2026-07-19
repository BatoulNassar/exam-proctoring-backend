using ExamProctoring.Domain.Entities;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IStudentRepository
    {
        Task<IEnumerable<Student>> GetAllAsync();
    }
}