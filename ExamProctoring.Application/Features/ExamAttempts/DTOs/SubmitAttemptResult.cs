namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// Outcome of a submit. Note there is no "already finalised" failure: a retry after a
    /// successful finalisation is a success that returns the frozen result, because a flaky
    /// network can lose the first response after the database has already committed.
    public enum SubmitAttemptOutcome
    {
        /// This call finalised the attempt. 200.
        Finalised,

        /// It was already terminal; the frozen result is returned. Also 200.
        AlreadyFinalised,

        /// The idempotency key had been used for a different submit request. 409.
        IdempotencyKeyReused,

        /// The supplied Idempotency-Key is missing or is not a usable UUID. 400.
        InvalidIdempotencyKey,

        AccountInactive,

        /// Attempt missing, never started, or owned by another student. Reported identically.
        AttemptNotFound,
    }

    public sealed class SubmitAttemptResult
    {
        public SubmitAttemptOutcome Outcome { get; init; }
        public SubmitAttemptResponse? Response { get; init; }

        public static SubmitAttemptResult Finalised(SubmitAttemptResponse response) =>
            new() { Outcome = SubmitAttemptOutcome.Finalised, Response = response };

        public static SubmitAttemptResult AlreadyFinalised(SubmitAttemptResponse response) =>
            new() { Outcome = SubmitAttemptOutcome.AlreadyFinalised, Response = response };

        public static SubmitAttemptResult Fail(SubmitAttemptOutcome outcome) =>
            new() { Outcome = outcome };
    }
}
