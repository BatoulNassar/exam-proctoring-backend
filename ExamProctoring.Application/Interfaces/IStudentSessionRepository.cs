using ExamProctoring.Application.Features.Eligibility.DTOs;
using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IStudentSessionRepository
    {
        /// Read-only projection of the student's non-deleted assignments joined to their
        /// non-deleted exam sessions, excluding DRAFT sessions, which are invisible to the
        /// student desktop client.
        Task<List<StudentAssignmentView>> GetVisibleAssignmentsAsync(int studentId);

        Task<StudentSession?> GetByIdAsync(int studentSessionId);

        /// Marks the entity modified. The caller commits through the unit of work.
        Task UpdateAsync(StudentSession studentSession);

        Task AddWarningMessageAsync(WarningMessage message);

        Task AddMonitoringEventAsync(MonitoringEvent monitoringEvent);

        Task AddAlertAsync(AlertEvent alert);
    }
}
