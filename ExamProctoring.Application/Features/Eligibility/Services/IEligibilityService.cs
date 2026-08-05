using ExamProctoring.Application.Features.Eligibility.DTOs;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Eligibility.Services
{
    public interface IEligibilityService
    {
        /// Answers "may this student start an exam right now?" entirely read-only.
        /// Revalidates the student against the database because the access token outlives
        /// any deactivation.
        Task<EligibilityResult> GetEligibilityAsync(int studentId);
    }
}
