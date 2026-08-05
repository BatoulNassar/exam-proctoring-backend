namespace ExamProctoring.Application.Features.Eligibility
{
    /// Stable machine-readable eligibility outcomes for the student desktop client.
    /// These are business results carried inside data.reasonCode, not HTTP error codes,
    /// and are never localized.
    public static class EligibilityReasonCodes
    {
        public const string NoSessionAssigned = "NO_SESSION_ASSIGNED";
        public const string SessionNotStarted = "SESSION_NOT_STARTED";
        public const string SessionClosed = "SESSION_CLOSED";
        public const string AlreadySubmitted = "ALREADY_SUBMITTED";
        public const string ExamAlreadyStarted = "EXAM_ALREADY_STARTED";
    }
}
