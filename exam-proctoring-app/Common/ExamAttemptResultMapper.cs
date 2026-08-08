using ExamProctoring.Application.Features.ExamAttempts;
using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Common
{
    /// Translates the Application's attempt outcomes into HTTP.
    ///
    /// This is API-layer work - it is the only place that knows an outcome means 409 rather than
    /// 404 - but it does not belong inside an action body, where it was four large switch
    /// statements. Every status code, stable code, message and details object here is carried over
    /// unchanged from the previous inline mapping.
    public static class ExamAttemptResultMapper
    {
        private const string AttemptNotFoundMessage = "Attempt was not found.";
        private const string AlreadyFinalisedMessage = "This attempt has already been finalised.";
        private const string KeyReusedMessage = "This idempotency key was already used for a different request.";

        /// Emitted when the Idempotency-Key header is missing or is not a usable UUID. The field
        /// name and wording are part of the client contract.
        private const string IdempotencyKeyField = "idempotencyKey";
        private const string IdempotencyKeyMessage = "An Idempotency-Key header containing a UUID is required.";

        public static IActionResult Map(StartAttemptResult result) => result.Outcome switch
        {
            StartAttemptOutcome.Started => ApiResults.Created(result.Response!, "Attempt started."),
            StartAttemptOutcome.Resumed => ApiResults.Ok(result.Response!, "Attempt resumed."),

            StartAttemptOutcome.AccountInactive => ApiResults.AccountInactive(),

            StartAttemptOutcome.AppVersionUnsupported => ApiResults.Fail(
                StatusCodes.Status426UpgradeRequired, AuthErrorCodes.AppVersionUnsupported,
                "The application version is no longer supported."),

            StartAttemptOutcome.DeviceMismatch => ApiResults.Fail(
                StatusCodes.Status403Forbidden, AuthErrorCodes.DeviceMismatch,
                "The device does not match the authenticated session."),

            StartAttemptOutcome.SessionNotFound => ApiResults.Fail(
                StatusCodes.Status404NotFound, AuthErrorCodes.SessionNotFound,
                "Exam session was not found."),

            StartAttemptOutcome.SessionNotActive => ApiResults.Fail(
                StatusCodes.Status403Forbidden, ExamAttemptErrorCodes.SessionNotActive,
                "The exam session does not currently permit this operation.",
                new { sessionStatus = result.SessionStatus }),

            StartAttemptOutcome.IdentityNotVerified => ApiResults.Fail(
                StatusCodes.Status403Forbidden, ExamAttemptErrorCodes.IdentityNotVerified,
                "Identity verification has not been completed for this exam session."),

            StartAttemptOutcome.AttemptDeviceConflict => ApiResults.Fail(
                StatusCodes.Status409Conflict, ExamAttemptErrorCodes.AttemptDeviceConflict,
                "This attempt is bound to a different device."),

            StartAttemptOutcome.AttemptAlreadyFinalised => ApiResults.Fail(
                StatusCodes.Status409Conflict, ExamAttemptErrorCodes.AttemptAlreadyFinalised,
                AlreadyFinalisedMessage),

            _ => ServerError(),
        };

        public static IActionResult Map(GetAttemptQuestionsResult result) => result.Outcome switch
        {
            GetAttemptQuestionsOutcome.Success => ApiResults.Ok(
                result.Response!, "Questions retrieved successfully."),

            GetAttemptQuestionsOutcome.AccountInactive => ApiResults.AccountInactive(),

            // Locked product decision: no paper after finalisation, and no exam review.
            GetAttemptQuestionsOutcome.AttemptAlreadyFinalised => ApiResults.Fail(
                StatusCodes.Status409Conflict, ExamAttemptErrorCodes.AttemptAlreadyFinalised,
                AlreadyFinalisedMessage),

            // A missing attempt and another student's attempt are reported identically, so attempt
            // ids cannot be probed for existence.
            _ => ApiResults.Fail(
                StatusCodes.Status404NotFound, ExamAttemptErrorCodes.AttemptNotFound, AttemptNotFoundMessage),
        };

        public static IActionResult Map(UpsertAnswerResult result) => result.Outcome switch
        {
            // A replay returns the frozen original payload, so a retry is indistinguishable from
            // the first call apart from being byte-identical.
            UpsertAnswerOutcome.Saved or UpsertAnswerOutcome.Replayed =>
                ApiResults.Ok(result.Response!, "Answer saved."),

            UpsertAnswerOutcome.AccountInactive => ApiResults.AccountInactive(),

            UpsertAnswerOutcome.InvalidIdempotencyKey => ApiResults.ValidationFailed(
                IdempotencyKeyField, IdempotencyKeyMessage),

            UpsertAnswerOutcome.ValidationFailed => ApiResults.ValidationFailed(
                "value", result.ValidationMessage ?? "The answer value is not acceptable."),

            UpsertAnswerOutcome.AnswerTypeMismatch => ApiResults.Fail(
                StatusCodes.Status400BadRequest, ExamAttemptErrorCodes.AnswerTypeMismatch,
                "The answer type does not match the question."),

            UpsertAnswerOutcome.AttemptNotFound => ApiResults.Fail(
                StatusCodes.Status404NotFound, ExamAttemptErrorCodes.AttemptNotFound, AttemptNotFoundMessage),

            UpsertAnswerOutcome.QuestionNotInAttempt => ApiResults.Fail(
                StatusCodes.Status404NotFound, ExamAttemptErrorCodes.QuestionNotInAttempt,
                "The question is not part of this attempt."),

            UpsertAnswerOutcome.AttemptAlreadyFinalised => ApiResults.Fail(
                StatusCodes.Status409Conflict, ExamAttemptErrorCodes.AttemptAlreadyFinalised,
                AlreadyFinalisedMessage),

            UpsertAnswerOutcome.AttemptTimeExpired => ApiResults.Fail(
                StatusCodes.Status409Conflict, ExamAttemptErrorCodes.AttemptTimeExpired,
                "The time allowed for this attempt has expired."),

            UpsertAnswerOutcome.IdempotencyKeyReused => ApiResults.Fail(
                StatusCodes.Status409Conflict, ExamAttemptErrorCodes.IdempotencyKeyReused, KeyReusedMessage),

            _ => ServerError(),
        };

        public static IActionResult Map(SubmitAttemptResult result) => result.Outcome switch
        {
            // Deliberately the same 200 for both: the frozen receipt is the source of truth, and an
            // already-finalised attempt is a success, not a conflict.
            SubmitAttemptOutcome.Finalised or SubmitAttemptOutcome.AlreadyFinalised =>
                ApiResults.Ok(result.Response!, "Attempt finalised."),

            SubmitAttemptOutcome.AccountInactive => ApiResults.AccountInactive(),

            SubmitAttemptOutcome.InvalidIdempotencyKey => ApiResults.ValidationFailed(
                IdempotencyKeyField, IdempotencyKeyMessage),

            SubmitAttemptOutcome.AttemptNotFound => ApiResults.Fail(
                StatusCodes.Status404NotFound, ExamAttemptErrorCodes.AttemptNotFound, AttemptNotFoundMessage),

            SubmitAttemptOutcome.IdempotencyKeyReused => ApiResults.Fail(
                StatusCodes.Status409Conflict, ExamAttemptErrorCodes.IdempotencyKeyReused, KeyReusedMessage),

            _ => ServerError(),
        };

        private static IActionResult ServerError() => ApiResults.Fail(
            StatusCodes.Status500InternalServerError, ExamAttemptErrorCodes.ServerError,
            "An unexpected error occurred.");
    }
}
