namespace ExamProctoring.Application.Features.Eligibility.DTOs
{
    public enum EligibilityStatus
    {
        /// A normal business outcome; the controller returns HTTP 200.
        Resolved,

        /// The token is valid but the student no longer exists, is soft-deleted, or is inactive.
        AccountInactive,

        /// More than one assignment has an open start window: a scheduling conflict, not a
        /// normal eligibility outcome.
        MultipleActiveSessions,
    }

    /// Outcome of an eligibility check. The controller maps this to an HTTP status,
    /// a stable error code and the response envelope.
    public class EligibilityResult
    {
        public EligibilityStatus Status { get; private set; }

        public EligibilityResponse? Response { get; private set; }

        public static EligibilityResult Resolved(EligibilityResponse response) =>
            new() { Status = EligibilityStatus.Resolved, Response = response };

        public static EligibilityResult AccountInactive() =>
            new() { Status = EligibilityStatus.AccountInactive };

        public static EligibilityResult MultipleActiveSessions() =>
            new() { Status = EligibilityStatus.MultipleActiveSessions };
    }
}
