using ExamProctoring.Domain.Entities;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IProctorActionRepository
    {
        Task AddAsync(ProctorAction action);

        /// <summary>
        /// Warnings already sent to this student in this session, counted across all
        /// of their alerts. Drives the automatic termination threshold.
        /// </summary>
        Task<int> CountWarningsForStudentSessionAsync(int studentSessionId);
    }
}
