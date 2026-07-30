using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IProctorSessionRepository
    {
        Task<IEnumerable<ProctorSession>> GetByExamSessionIdAsync(int examSessionId);
        Task<ProctorSession?> GetByExamSessionAndProctorAsync(int examSessionId, int proctorId);
        Task AddAsync(ProctorSession proctorSession);
        Task DeleteAsync(ProctorSession proctorSession);
    }
}
