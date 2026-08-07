using ExamProctoring.Domain.Entities;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IDeviceCheckRepository
    {
        /// Persists the report and all of its requirement rows in a single save, so a
        /// parent can never be stored without its children. Returns the new identifier.
        Task<int> AddAsync(DeviceCheck deviceCheck);
    }
}
