namespace ExamProctoring.Application.Features.ExamAttempts
{
    /// Stable machine-readable codes for the exam-attempt endpoints. These values are part of
    /// the API contract and are never localized.
    ///
    /// Codes already defined for authentication (VALIDATION_FAILED, APP_VERSION_UNSUPPORTED,
    /// ACCOUNT_INACTIVE, DEVICE_MISMATCH, SESSION_NOT_FOUND) are reused from AuthErrorCodes
    /// rather than duplicated here, so one cause never has two codes.
    public static class ExamAttemptErrorCodes
    {
        /// The exam session exists and is assigned, but its lifecycle does not currently
        /// permit this operation (too early, login window closed, GRACE, CLOSED, ARCHIVED).
        public const string SessionNotActive = "SESSION_NOT_ACTIVE";

        /// Identity verification has not been completed for this exam session.
        public const string IdentityNotVerified = "IDV_NOT_VERIFIED";

        /// The token's device is valid but differs from the device this attempt was bound to.
        public const string AttemptDeviceConflict = "ATTEMPT_DEVICE_CONFLICT";

        /// The attempt does not exist, has not started, or belongs to another student.
        /// Deliberately identical in all three cases so attempt ids cannot be probed.
        public const string AttemptNotFound = "ATTEMPT_NOT_FOUND";

        /// The attempt has already been submitted or terminated.
        public const string AttemptAlreadyFinalised = "ATTEMPT_ALREADY_FINALISED";

        /// The server clock has reached the attempt's personal deadline. The client's own
        /// timestamp never changes this answer.
        public const string AttemptTimeExpired = "ATTEMPT_TIME_EXPIRED";

        /// The question is not part of this attempt's materialised paper.
        public const string QuestionNotInAttempt = "QUESTION_NOT_IN_ATTEMPT";

        /// value.type does not match the type of the question being answered.
        public const string AnswerTypeMismatch = "ANSWER_TYPE_MISMATCH";

        /// The Idempotency-Key was already used for a semantically different request.
        public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_BODY";

        /// Unexpected server-side failure.
        public const string ServerError = "SERVER_ERROR";
    }
}
