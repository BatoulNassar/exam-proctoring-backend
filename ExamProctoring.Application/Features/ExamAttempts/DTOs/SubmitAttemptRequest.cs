namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// Body of POST .../attempts/{attemptId}/submit, per contract section 6.2.
    public class SubmitAttemptRequest
    {
        /// STUDENT_SUBMIT | CLIENT_TIMER_EXPIRED | PROCTOR_TERMINATED | CONNECTIVITY_RECOVERY
        public string? Reason { get; set; }

        /// Device clock, ISO-8601 with an explicit UTC designator. Audit only - it never
        /// influences finalisedAtUtc.
        public string? ClientTimeUtc { get; set; }

        /// Contract-permitted body duplicate of the Idempotency-Key header. The header wins.
        public string? ClientMutationId { get; set; }
    }

    /// Response body for a finalised attempt, per contract section 6.3.
    /// Every field is read from frozen persisted state, so this object is identical on the
    /// first call and on every retry thereafter.
    public class SubmitAttemptResponse
    {
        public System.Guid AttemptId { get; set; }

        /// SUBMITTED | TERMINATED | EXPIRED
        public string Status { get; set; } = string.Empty;

        public System.DateTime FinalisedAtUtc { get; set; }

        /// The only field that legitimately differs between the original call and a replay,
        /// because it reports when the server answered rather than when the attempt ended.
        public System.DateTime ServerTimeUtc { get; set; }

        /// Questions with a non-empty answer at finalisation.
        public int AnsweredCount { get; set; }

        /// Total questions in the student's materialised paper, frozen at Start.
        public int QuestionCount { get; set; }

        public string ReceiptCode { get; set; } = string.Empty;

        /// The frozen grading snapshot. Required on every 2xx submit response, including
        /// idempotent replays and already-finalised soft-200s: the client deletes its in-memory
        /// paper when it leaves the exam, so this response is the only thing it can render the
        /// receipt from.
        public GradingSnapshotDto Grading { get; set; } = new();
    }
}
