using ExamProctoring.Application.Features.DeviceChecks.DTOs;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.DeviceChecks.Services
{
    public interface IDeviceCheckService
    {
        /// Records a client-reported device-readiness report against the student's own
        /// assignment. Read-only with respect to StudentSession and ExamSession: it only
        /// appends a new DeviceCheck history row.
        Task<DeviceCheckResult> RecordAsync(DeviceCheckRequest request, int studentId, string? deviceIdClaim);
    }
}
