namespace ExamProctoring.Domain.Enums
{
    /// Why an attempt reached its terminal state. Persisted as a string, so members are
    /// append-only.
    ///
    /// This is the durable source of truth for how an attempt ended - deliberately a typed
    /// column rather than free-text audit detail, because the submit response's status is
    /// derived from it and must stay stable forever.
    public enum AttemptFinalisationReason
    {
        /// The student pressed Submit.
        StudentSubmit = 1,

        /// The client's own countdown reached zero and it submitted explicitly.
        ClientTimerExpired = 2,

        /// The client submitted after a proctor terminated the attempt.
        ProctorTerminated = 3,

        /// Last-chance flush after a connectivity gap.
        ConnectivityRecovery = 4,

        /// The server finalised the attempt itself because the personal deadline passed with no
        /// explicit submit. Distinct from ClientTimerExpired: nobody submitted at all, which is
        /// what makes the API report EXPIRED rather than SUBMITTED.
        ServerExpiry = 5,
    }
}
