namespace ExamProctoring.Application.Features.IdentityVerification
{
    /// Stable machine-readable codes for the identity verification endpoints, taken verbatim
    /// from IDENTITY_VERIFICATION_API_CONTRACT.md. Never localized.
    ///
    /// Codes already defined elsewhere are reused rather than restated, so one cause never has
    /// two codes: ACCOUNT_INACTIVE and VALIDATION_FAILED come from AuthErrorCodes, and
    /// SERVER_ERROR from ExamAttemptErrorCodes.
    public static class IdentityVerificationErrorCodes
    {
        /// Contract §2.3 - the student is not admitted to an active exam session, so there is
        /// nothing to verify against. Also returned when eligibility resolves to no session at
        /// all, so a missing assignment cannot be distinguished from an unopened one.
        public const string SessionNotAdmitted = "SESSION_NOT_ADMITTED";

        /// Contract §2.3 - identity is already confirmed for this exam session; the client
        /// should skip ahead to the exam rather than re-verify.
        public const string AlreadyVerified = "IDV_ALREADY_VERIFIED";

        /// Contract §3.5 - wrong length, non-finite values, or a degenerate vector.
        /// Never consumes an attempt: nothing was compared.
        public const string EmbeddingInvalid = "IDV_EMBEDDING_INVALID";

        /// Contract §3.5 - the submitted model/version differs from this backend's pinned pair
        /// or from the enrolled reference. Never consumes an attempt.
        public const string ModelMismatch = "IDV_MODEL_MISMATCH";

        /// Contract §3.5 - the attempt budget for this verification session is exhausted.
        public const string NoAttemptsRemaining = "IDV_NO_ATTEMPTS_REMAINING";

        /// The verification session does not exist, or belongs to another student.
        /// Deliberately identical in both cases so ids cannot be probed for existence.
        ///
        /// Not named in the contract, which lists no 404 for this route; a route with an id
        /// segment needs one, and this follows the ATTEMPT_NOT_FOUND precedent already in the
        /// exam-attempt contract rather than inventing a new shape.
        public const string SessionNotFound = "IDV_SESSION_NOT_FOUND";
    }
}
