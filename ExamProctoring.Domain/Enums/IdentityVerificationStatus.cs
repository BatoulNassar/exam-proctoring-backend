namespace ExamProctoring.Domain.Enums
{
    /// Lifecycle of one student's identity verification for one exam assignment.
    ///
    /// Deliberately only two states. "Exhausted" is NOT a state: it is derived from
    /// attempts_used >= max_attempts, so the counter stays the single source of truth and
    /// cannot disagree with a status column. Persisted as a string.
    public enum IdentityVerificationStatus
    {
        /// Created, not yet verified. Attempts may still be submitted while the budget lasts.
        Pending = 1,

        /// Terminal. A FACE_MATCH succeeded and the result is persisted on StudentSession.
        Verified = 2,
    }
}
