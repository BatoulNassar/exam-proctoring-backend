using ExamProctoring.Application.Features.IdentityVerification.DTOs;
using System;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.IdentityVerification.Services
{
    /// The two student-facing identity verification use cases (FR-01).
    ///
    /// Everything the endpoints decide - admission, attempt budget, idempotency, liveness
    /// plausibility, the face comparison and the threshold - lives behind this interface. The
    /// controller only binds HTTP and maps outcomes.
    public interface IIdentityVerificationService
    {
        /// Creates or resumes verification for the student's currently admitted exam session.
        Task<CreateVerificationSessionResult> CreateOrResumeAsync(int studentId, string? deviceIdClaim);

        /// Submits one verification attempt against an existing verification session.
        Task<SubmitVerificationAttemptResult> SubmitAttemptAsync(
            SubmitVerificationAttemptRequest request, int studentId, Guid verificationSessionId);
    }
}
