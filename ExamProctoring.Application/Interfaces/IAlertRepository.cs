using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IAlertRepository
    {
        Task<IList<AlertEvent>> GetRecentPagedAsync(int page, int pageSize);
        Task<int> CountOpenAlertsAsync();
    }
}
