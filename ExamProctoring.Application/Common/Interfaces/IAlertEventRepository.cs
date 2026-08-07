using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IAlertEventRepository
    {
        Task<AlertEvent> GetByIdAsync(int id);
        Task<IEnumerable<AlertEvent>> GetAllAsync();
        /// <param name="restrictToSessionIds">
        /// When given, results never reach outside these exam sessions. An empty
        /// collection yields no rows. This is an authorization boundary, not a filter.
        /// </param>
        Task<IEnumerable<AlertEvent>> GetAlertsByFilterAsync(AlertStatus? status, string alertType, int? sessionId, IReadOnlyCollection<int> restrictToSessionIds = null);
        Task AddAsync(AlertEvent alert);
        Task UpdateAsync(AlertEvent alert);
    }
}
