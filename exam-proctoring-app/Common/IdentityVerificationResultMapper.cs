using ExamProctoring.Application.Features.ExamAttempts;
using ExamProctoring.Application.Features.IdentityVerification;
using ExamProctoring.Application.Features.IdentityVerification.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Common
{
    /// Translates the Application's identity outcomes into HTTP, following the same pattern as
    /// ExamAttemptResultMapper: this is the only place that knows an outcome means 409 rather
    /// than 403, and it deliberately does not live inside an action body.
    ///
    /// Status codes and stable codes are taken from IDENTITY_VERIFICATION_API_CONTRACT.md.
    /// Where that contract conflicts with API_CONTRACT.md's older summary section, the
    /// specialized contract wins.
    public static class IdentityVerificationResultMapper
    {
        private const string SessionNotFoundMessage = "Verification session was not found.";

        public static IActionResult Map(CreateVerificationSessionResult result) => result.Outcome switch
        {
            CreateVerificationSessionOutcome.Created =>
                ApiResults.Created(result.Response!, "Verification session created."),

            CreateVerificationSessionOutcome.Resumed =>
                ApiResults.Ok(result.Response!, "Verification session resumed."),

            CreateVerificationSessionOutcome.AccountInactive => ApiResults.AccountInactive(),

            CreateVerificationSessionOutcome.SessionNotAdmitted => ApiResults.Fail(
                StatusCodes.Status403Forbidden, IdentityVerificationErrorCodes.SessionNotAdmitted,
                "The student is not admitted to an active exam session."),

            CreateVerificationSessionOutcome.AlreadyVerified => ApiResults.Fail(
                StatusCodes.Status409Conflict, IdentityVerificationErrorCodes.AlreadyVerified,
                "Identity has already been verified for this exam session."),

            _ => ServerError(),
        };

        public static IActionResult Map(SubmitVerificationAttemptResult result) => result.Outcome switch
        {
            // Deliberately the same 200 for both. A replay is indistinguishable from the first
            // call apart from being identical, which is the entire point of clientAttemptId:
            // a dropped response must never cost a student an attempt.
            SubmitVerificationAttemptOutcome.Completed or SubmitVerificationAttemptOutcome.Replayed =>
                ApiResults.Ok(result.Response!, "Verification attempt processed."),

            SubmitVerificationAttemptOutcome.AccountInactive => ApiResults.AccountInactive(),

            // A missing verification session and another student's are reported identically,
            // so ids cannot be probed for existence.
            SubmitVerificationAttemptOutcome.SessionNotFound => ApiResults.Fail(
                StatusCodes.Status404NotFound, IdentityVerificationErrorCodes.SessionNotFound,
                SessionNotFoundMessage),

            SubmitVerificationAttemptOutcome.EmbeddingInvalid => ApiResults.Fail(
                StatusCodes.Status400BadRequest, IdentityVerificationErrorCodes.EmbeddingInvalid,
                "The submitted face embedding is not valid."),

            SubmitVerificationAttemptOutcome.ModelMismatch => ApiResults.Fail(
                StatusCodes.Status409Conflict, IdentityVerificationErrorCodes.ModelMismatch,
                "The embedding model or version does not match the enrolled reference."),

            SubmitVerificationAttemptOutcome.AlreadyVerified => ApiResults.Fail(
                StatusCodes.Status409Conflict, IdentityVerificationErrorCodes.AlreadyVerified,
                "Identity has already been verified for this exam session."),

            SubmitVerificationAttemptOutcome.NoAttemptsRemaining => ApiResults.Fail(
                StatusCodes.Status409Conflict, IdentityVerificationErrorCodes.NoAttemptsRemaining,
                "No verification attempts remain."),

            // A missing threshold is a server configuration fault, not something the student
            // did. It is reported as SERVER_ERROR with no detail, because naming the missing
            // setting to a student's machine tells an attacker the comparison is disabled.
            SubmitVerificationAttemptOutcome.ThresholdNotConfigured => ServerError(),

            _ => ServerError(),
        };

        private static IActionResult ServerError() => ApiResults.Fail(
            StatusCodes.Status500InternalServerError, ExamAttemptErrorCodes.ServerError,
            "An unexpected error occurred.");
    }
}
