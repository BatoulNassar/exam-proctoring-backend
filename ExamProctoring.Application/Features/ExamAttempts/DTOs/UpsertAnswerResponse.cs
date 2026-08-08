using System;

namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// Response body for an accepted answer write, per contract section 5.3.
    /// This exact object is what gets frozen into the idempotency record and replayed
    /// verbatim on a retry - including the original serverTimeUtc.
    public class UpsertAnswerResponse
    {
        /// The per-attempt question public id from the route.
        public Guid QuestionId { get; set; }

        /// Server clock of acceptance. Source of truth for "last write".
        public DateTime SavedAtUtc { get; set; }

        /// Lets the client re-sync its countdown after a connectivity gap.
        public DateTime ServerTimeUtc { get; set; }

        /// Echoed so the client recovers the deadline after a long offline period.
        public DateTime EndsAtUtc { get; set; }

        /// The stored value, after clamping.
        public int DurationMs { get; set; }

        /// IN_PROGRESS | SUBMITTED | TERMINATED | EXPIRED
        public string AttemptStatus { get; set; } = string.Empty;
    }

    /// One persisted answer as returned by GET questions (nested under its question).
    public class SavedAnswerContentDto
    {
        public AnswerValueDto Value { get; set; } = new();
        public int DurationMs { get; set; }
        public DateTime SavedAtUtc { get; set; }
    }
}
