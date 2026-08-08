namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// Outcome of an answer write. The controller maps this to an HTTP status and a stable
    /// error code; the service never references HTTP.
    public enum UpsertAnswerOutcome
    {
        /// Written for the first time with this key. 200.
        Saved,

        /// The same key and the same semantic request; the original response is replayed. 200.
        Replayed,

        /// The same key with a different semantic request. 409.
        IdempotencyKeyReused,

        /// The supplied Idempotency-Key is missing or is not a usable UUID. 400.
        InvalidIdempotencyKey,

        AccountInactive,

        /// Attempt missing, not started, or owned by another student. All reported identically.
        AttemptNotFound,

        /// The question is not part of this attempt's materialised paper.
        QuestionNotInAttempt,

        /// value.type disagrees with the attempt question's type.
        AnswerTypeMismatch,

        /// Option ids, cardinality, or text length are not acceptable for this question.
        ValidationFailed,

        /// The attempt is submitted or terminated.
        AttemptAlreadyFinalised,

        /// Server clock has reached the personal deadline.
        AttemptTimeExpired,
    }

    public sealed class UpsertAnswerResult
    {
        public UpsertAnswerOutcome Outcome { get; init; }
        public UpsertAnswerResponse? Response { get; init; }

        /// Field-level detail for ValidationFailed, in the same shape the other student
        /// endpoints use.
        public string? ValidationMessage { get; init; }

        public static UpsertAnswerResult Saved(UpsertAnswerResponse response) =>
            new() { Outcome = UpsertAnswerOutcome.Saved, Response = response };

        public static UpsertAnswerResult Replayed(UpsertAnswerResponse response) =>
            new() { Outcome = UpsertAnswerOutcome.Replayed, Response = response };

        public static UpsertAnswerResult Invalid(string message) =>
            new() { Outcome = UpsertAnswerOutcome.ValidationFailed, ValidationMessage = message };

        public static UpsertAnswerResult Fail(UpsertAnswerOutcome outcome) =>
            new() { Outcome = outcome };
    }
}
