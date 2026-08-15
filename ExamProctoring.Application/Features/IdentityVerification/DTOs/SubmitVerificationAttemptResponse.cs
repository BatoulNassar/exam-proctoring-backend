namespace ExamProctoring.Application.Features.IdentityVerification.DTOs
{
    /// Body of a completed verification attempt - contract §3.3. Always HTTP 200: a non-match
    /// is the normal output of a working comparison, not a transport error.
    public class SubmitVerificationAttemptResponse
    {
        /// MATCHED | NOT_MATCHED | LIVENESS_REJECTED | NO_ENROLLED_FACE
        public string Outcome { get; set; } = string.Empty;

        public int AttemptNumber { get; set; }

        /// Authoritative - the client displays this rather than counting itself, so the two
        /// can never drift apart.
        public int AttemptsRemaining { get; set; }

        /// Cosine similarity 0..1, for display and threshold calibration. Null when no
        /// comparison ran (liveness rejected, or no enrolled face); the contract states the
        /// client handles null, and it never compares this to a threshold itself.
        public double? MatchScore { get; set; }
    }
}
