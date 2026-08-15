using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.ExamAttempts;
using ExamProctoring.Application.Features.IdentityVerification.DTOs;
using ExamProctoring.Application.Features.IdentityVerification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    /// Student identity verification (FR-01), called by the Flutter Windows client after the
    /// device check and before the exam attempt starts.
    ///
    /// The client runs face detection and the blink challenge locally and sends only a
    /// 128-value SFace embedding plus liveness evidence - no camera frame ever reaches this
    /// API. The verdict is decided here, never on the client: a client that decided locally
    /// could be patched to admit anyone.
    ///
    /// Student tokens only; dashboard tokens are rejected by the StudentOnly policy.
    ///
    /// Every action is transport only - bind, read claims, call one use case, map the result.
    /// No decision about admission, attempt budgets, idempotency, liveness, thresholds or
    /// vector similarity is made in this file.
    [ApiController]
    [Route("api/v1/identity/verification-sessions")]
    [Authorize(Policy = AuthorizationPolicies.StudentOnly)]
    [ValidateRequest]
    [ApiExceptionFilter(Code = ExamAttemptErrorCodes.ServerError)]
    public class IdentityVerificationController : ControllerBase
    {
        /// A 128-float JSON array is ~2.5 KB. This leaves generous headroom for the liveness
        /// block while refusing a body that could only be an attack or a bug.
        private const int MaxAttemptRequestBytes = 64 * 1024;

        private readonly IIdentityVerificationService _identityVerificationService;

        public IdentityVerificationController(IIdentityVerificationService identityVerificationService)
        {
            _identityVerificationService = identityVerificationService;
        }

        /// Creates verification for the student's admitted exam session, or resumes the
        /// existing one. 201 the first time, 200 for every later call - and a resume returns
        /// the same id and the same attempt count, so an app crash after two failures does not
        /// grant a fresh three.
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateOrResume()
        {
            if (!User.TryGetStudentId(out var studentId))
                return ApiResults.AccountInactive();

            var result = await _identityVerificationService.CreateOrResumeAsync(
                studentId, User.GetDeviceId());

            return IdentityVerificationResultMapper.Map(result);
        }

        /// Submits one verification attempt. Idempotent on the body's clientAttemptId; the key
        /// is bound here, but what a replay means and whether an attempt is consumed are
        /// decided in the Application layer.
        ///
        /// Always 200 once a comparison completes - MATCHED, NOT_MATCHED, LIVENESS_REJECTED
        /// and NO_ENROLLED_FACE are business outcomes in the body, not HTTP errors, so a
        /// failed match stays distinguishable from a transport failure.
        [HttpPost("{verificationSessionId:guid}/attempts")]
        [RequestSizeLimit(MaxAttemptRequestBytes)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitAttempt(
            Guid verificationSessionId, [FromBody] SubmitVerificationAttemptRequest request)
        {
            if (!User.TryGetStudentId(out var studentId))
                return ApiResults.AccountInactive();

            var result = await _identityVerificationService.SubmitAttemptAsync(
                request, studentId, verificationSessionId);

            return IdentityVerificationResultMapper.Map(result);
        }
    }
}
