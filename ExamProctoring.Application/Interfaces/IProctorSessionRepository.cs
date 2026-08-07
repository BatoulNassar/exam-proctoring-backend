using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IProctorSessionRepository
    {
        Task<IEnumerable<ProctorSession>> GetByExamSessionIdAsync(int examSessionId);
        Task<ProctorSession?> GetByExamSessionAndProctorAsync(int examSessionId, int proctorId);

        /// <summary>
        /// Ids of the exam sessions this proctor is assigned to. This is the
        /// visibility boundary for everything a proctor is allowed to see.
        /// </summary>
        Task<IReadOnlyList<int>> GetSessionIdsByProctorAsync(int proctorId);
        Task AddAsync(ProctorSession proctorSession);
        Task DeleteAsync(ProctorSession proctorSession);
    }
}
