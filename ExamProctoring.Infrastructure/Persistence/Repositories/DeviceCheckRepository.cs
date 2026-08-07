using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class DeviceCheckRepository : IDeviceCheckRepository
    {
        private readonly AppDbContext _context;

        public DeviceCheckRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddAsync(DeviceCheck deviceCheck)
        {
            // The requirement rows travel in the navigation collection, so EF Core writes
            // the parent and every child inside one SaveChanges transaction.
            await _context.DeviceChecks.AddAsync(deviceCheck);
            await _context.SaveChangesAsync();

            return deviceCheck.id;
        }
    }
}
