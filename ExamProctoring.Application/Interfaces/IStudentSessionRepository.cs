using ExamProctoring.Application.Features.Eligibility.DTOs;
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
    }
}
