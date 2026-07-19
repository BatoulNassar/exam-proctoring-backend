using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}
