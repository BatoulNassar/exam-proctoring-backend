using ExamProctoring.Domain.Entities;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IQuestionBankRepository
    {
        Task<int> CountAsync();
        Task<bool> ExistsAsync(int id);
        Task<QuestionBank> GetByIdAsync(int id);
        Task<IEnumerable<QuestionBank>> GetAllAsync();
        Task AddAsync(QuestionBank questionBank);
        Task UpdateAsync(QuestionBank questionBank);
        Task DeleteAsync(QuestionBank questionBank);
    }
}
