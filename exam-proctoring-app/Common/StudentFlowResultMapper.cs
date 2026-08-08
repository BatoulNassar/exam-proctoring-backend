using ExamProctoring.Application.Features.DeviceChecks.DTOs;
using ExamProctoring.Application.Features.Eligibility.DTOs;
using ExamProctoring.Application.Features.StudentAuth.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Common
{
    /// HTTP mapping for the pre-exam student flows: login, eligibility and device check.
    /// Every status, stable code, message and details object is carried over unchanged from the
    /// previous inline switches.
    public static class StudentFlowResultMapper
    {
        /// <param name="response">
        /// Needed only to set Retry-After on a lockout - a genuine HTTP transport concern that has
        /// no place in the Application layer, and the one reason this mapper takes the response.
        /// </param>
        public static IActionResult MapLogin(StudentLoginResult result, HttpResponse response)
        {
            switch (result.Status)
            {
                case StudentLoginStatus.Success:
                    return ApiResults.Ok(result.Response!, "Login successful.");

                case StudentLoginStatus.AppVersionUnsupported:
                    return ApiResults.Fail(
                        StatusCodes.Status426UpgradeRequired, AuthErrorCodes.AppVersionUnsupported,
                        "Application update is required.",
                        new { minimumVersion = result.MinimumVersion });

                case StudentLoginStatus.AccountLocked:
                    response.Headers["Retry-After"] = result.RetryAfterSeconds!.Value.ToString();
                    return ApiResults.Fail(
                        StatusCodes.Status423Locked, AuthErrorCodes.AccountLocked,
                        "Account is temporarily locked.",
                        new { retryAfterSeconds = result.RetryAfterSeconds });

                case StudentLoginStatus.AccountInactive:
                    return ApiResults.AccountInactive();

                default:
                    return ApiResults.Fail(
                        StatusCodes.Status401Unauthorized, AuthErrorCodes.InvalidCredentials,
                        "Invalid identifier or password.",
                        new { remainingAttempts = result.RemainingAttempts });
            }
        }

        public static IActionResult MapEligibility(EligibilityResult result) => result.Status switch
        {
            EligibilityStatus.AccountInactive => ApiResults.AccountInactive(),

            EligibilityStatus.MultipleActiveSessions => ApiResults.Fail(
                StatusCodes.Status409Conflict, AuthErrorCodes.MultipleActiveSessions,
                "Multiple active exam sessions are assigned to the student."),

            // Not being eligible is not an error: the client renders the reason, so this stays 200.
            _ => ApiResults.Ok(result.Response!, "Eligibility checked successfully."),
        };

        public static IActionResult MapDeviceCheck(DeviceCheckResult result) => result switch
        {
            // Fire-and-forget from the client's perspective: success is 202 with no body.
            DeviceCheckResult.Accepted => new StatusCodeResult(StatusCodes.Status202Accepted),

            DeviceCheckResult.AccountInactive => ApiResults.AccountInactive(),

            DeviceCheckResult.DeviceMismatch => ApiResults.Fail(
                StatusCodes.Status403Forbidden, AuthErrorCodes.DeviceMismatch,
                "The device does not match the authenticated session."),

            _ => ApiResults.Fail(
                StatusCodes.Status404NotFound, AuthErrorCodes.SessionNotFound,
                "Exam session was not found."),
        };
    }
}
