namespace ExamProctoring.Application.Features.IdentityVerification.DTOs
{
    public enum CreateVerificationSessionOutcome
    {
        /// A new verification session was created - HTTP 201.
        Created,

        /// An existing one was returned unchanged, preserving its attempt count - HTTP 200.
        Resumed,

        /// Token is valid but the student is missing, soft-deleted or inactive.
        AccountInactive,

        /// Not admitted to an active exam session, or no assignment at all.
        SessionNotAdmitted,

        /// Identity is already confirmed for this exam session.
        AlreadyVerified,
    }

    public class CreateVerificationSessionResult
    {
        public CreateVerificationSessionOutcome Outcome { get; private set; }

        public VerificationSessionResponse? Response { get; private set; }

        public static CreateVerificationSessionResult Created(VerificationSessionResponse response) =>
            new() { Outcome = CreateVerificationSessionOutcome.Created, Response = response };

        public static CreateVerificationSessionResult Resumed(VerificationSessionResponse response) =>
            new() { Outcome = CreateVerificationSessionOutcome.Resumed, Response = response };

        public static CreateVerificationSessionResult Fail(CreateVerificationSessionOutcome outcome) =>
            new() { Outcome = outcome };
    }

    public enum SubmitVerificationAttemptOutcome
    {
        /// The attempt was processed - HTTP 200 with a business outcome in the body.
        /// Covers MATCHED, NOT_MATCHED, LIVENESS_REJECTED and NO_ENROLLED_FACE.
        Completed,

        /// A retry of an already-recorded clientAttemptId, replayed verbatim - HTTP 200.
        Replayed,

        AccountInactive,

        /// Unknown verification session, or one belonging to another student.
        SessionNotFound,

        /// Wrong length, non-finite values, or a degenerate vector. Consumes no attempt.
        EmbeddingInvalid,

        /// Submitted model/version is not the pinned pair, or differs from the enrolled
        /// reference. Consumes no attempt.
        ModelMismatch,

        /// Identity is already confirmed for this exam session.
        AlreadyVerified,

        /// The attempt budget is exhausted.
        NoAttemptsRemaining,

        /// The SFace cosine threshold is not configured. Fails closed rather than comparing
        /// against a guessed number.
        ThresholdNotConfigured,
    }

    public class SubmitVerificationAttemptResult
    {
        public SubmitVerificationAttemptOutcome Outcome { get; private set; }

        public SubmitVerificationAttemptResponse? Response { get; private set; }

        public static SubmitVerificationAttemptResult Completed(SubmitVerificationAttemptResponse response) =>
            new() { Outcome = SubmitVerificationAttemptOutcome.Completed, Response = response };

        public static SubmitVerificationAttemptResult Replayed(SubmitVerificationAttemptResponse response) =>
            new() { Outcome = SubmitVerificationAttemptOutcome.Replayed, Response = response };

        public static SubmitVerificationAttemptResult Fail(SubmitVerificationAttemptOutcome outcome) =>
            new() { Outcome = outcome };
    }
}
