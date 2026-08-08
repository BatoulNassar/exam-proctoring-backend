namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// Outcome of Start/Resume. The controller maps this to an HTTP status and a stable error
    /// code; the service itself never references HTTP.
    public enum StartAttemptOutcome
    {
        /// A brand-new attempt was claimed and its paper materialised. Controller returns 201.
        Started,

        /// The attempt already existed. Controller returns 200 with identical state.
        Resumed,

        /// The token is valid but the student is missing, soft-deleted or inactive.
        AccountInactive,

        /// The client build is below the configured minimum supported version.
        AppVersionUnsupported,

        /// The request device does not match the authenticated token's device_id, or the token
        /// carries no usable device_id.
        DeviceMismatch,

        /// The session does not exist, is not assigned to this student, is soft-deleted or is
        /// still DRAFT. Reported identically in every case.
        SessionNotFound,

        /// The session lifecycle does not permit this operation right now.
        SessionNotActive,

        /// Identity verification has not been completed for this exam session.
        IdentityNotVerified,

        /// The attempt is bound to a different device.
        AttemptDeviceConflict,

        /// The attempt has already been submitted or terminated.
        AttemptAlreadyFinalised,
    }

    public sealed class StartAttemptResult
    {
        public StartAttemptOutcome Outcome { get; init; }
        public StartAttemptResponse? Response { get; init; }

        /// Present on SessionNotActive so the client can explain which state blocked it.
        public string? SessionStatus { get; init; }

        public static StartAttemptResult Started(StartAttemptResponse response) =>
            new() { Outcome = StartAttemptOutcome.Started, Response = response };

        public static StartAttemptResult Resumed(StartAttemptResponse response) =>
            new() { Outcome = StartAttemptOutcome.Resumed, Response = response };

        public static StartAttemptResult Fail(StartAttemptOutcome outcome, string? sessionStatus = null) =>
            new() { Outcome = outcome, SessionStatus = sessionStatus };
    }

    public enum GetAttemptQuestionsOutcome
    {
        Success,

        /// The attempt does not exist, has not started, or belongs to another student.
        AttemptNotFound,

        /// The attempt is terminal. The paper is deliberately no longer served.
        AttemptAlreadyFinalised,

        AccountInactive,
    }

    public sealed class GetAttemptQuestionsResult
    {
        public GetAttemptQuestionsOutcome Outcome { get; init; }
        public AttemptQuestionsResponse? Response { get; init; }

        public static GetAttemptQuestionsResult Success(AttemptQuestionsResponse response) =>
            new() { Outcome = GetAttemptQuestionsOutcome.Success, Response = response };

        public static GetAttemptQuestionsResult Fail(GetAttemptQuestionsOutcome outcome) =>
            new() { Outcome = outcome };
    }
}
