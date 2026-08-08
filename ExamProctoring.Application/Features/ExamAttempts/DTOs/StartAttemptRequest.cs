namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// Body of POST /exam-sessions/{examSessionId}/attempts/start.
    /// Every value is client-supplied; none of it is trusted for timing decisions.
    public class StartAttemptRequest
    {
        /// UUID of the installation; must match the authenticated token's device_id claim.
        public string? DeviceId { get; set; }

        /// Flutter release version, e.g. "1.0.0+1". Refused when below the configured minimum.
        public string? AppVersion { get; set; }

        /// Client clock reading, ISO-8601 with an explicit UTC designator. Audit and skew
        /// diagnostics only - it never influences startedAtUtc, endsAtUtc or eligibility.
        /// Bound as a string so a malformed value is reported through the project's
        /// ApiResponse envelope instead of framework ProblemDetails.
        public string? ClientTimeUtc { get; set; }
    }
}
