using ExamProctoring.Application.Features.StudentAuth.DTOs;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.StudentAuth.Services
{
    public interface IStudentAuthService
    {
        /// Authenticates a student against the Student table only, applying the client-version gate,
        /// lockout policy and active-account rules. An unknown identifier and a wrong password are
        /// reported identically so account existence is never disclosed.
        Task<StudentLoginResult> LoginAsync(StudentLoginRequest request);
    }
}
