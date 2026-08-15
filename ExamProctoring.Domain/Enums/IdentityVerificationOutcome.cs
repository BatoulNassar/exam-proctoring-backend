namespace ExamProctoring.Domain.Enums
{
    /// Result of one completed verification attempt.
    ///
    /// These are business outcomes carried in a 200 response body, never HTTP errors:
    /// a non-match is the normal output of a working comparison, and modelling it as a 4xx
    /// would make it indistinguishable from a transport failure - which must never consume
    /// an attempt. Persisted as a string; the names are part of the client contract.
    public enum IdentityVerificationOutcome
    {
        /// At or above the configured SFace cosine threshold. Terminal success.
        MATCHED = 1,

        /// Below the threshold. Retryable while attempts remain.
        NOT_MATCHED = 2,

        /// The submitted liveness evidence was not accepted. Retryable while attempts remain.
        LIVENESS_REJECTED = 3,

        /// The student has no trusted reference embedding on file. Not retryable, and it
        /// deliberately consumes no attempt - nothing was compared.
        NO_ENROLLED_FACE = 4,
    }
}
