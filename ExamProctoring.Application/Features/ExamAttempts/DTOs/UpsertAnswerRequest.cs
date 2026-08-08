using System;
using System.Collections.Generic;

namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// Body of PUT .../attempts/{attemptId}/answers/{questionId}.
    /// Field names follow EXAM_SESSION_API_CONTRACT.md section 5.2 exactly.
    public class UpsertAnswerRequest
    {
        /// Discriminated by Value.Type; must match the attempt question's type.
        public AnswerValueDto? Value { get; set; }

        /// Cumulative focused milliseconds for this question. Telemetry only; clamped, never
        /// rejected outright for being implausible.
        public int? DurationMs { get; set; }

        /// Device clock, ISO-8601 with an explicit UTC designator. Audit only.
        /// Bound as a string so a malformed value returns the project's ApiResponse envelope
        /// rather than framework ProblemDetails.
        public string? ClientAnsweredAtUtc { get; set; }

        /// Contract-permitted body duplicate of the Idempotency-Key header, for clients that
        /// can only pack bodies (offline buffer replay). When both are present the header wins.
        public string? ClientMutationId { get; set; }
    }

    /// The student's submitted answer content. Exactly one of OptionIds / Text applies,
    /// decided by Type.
    public class AnswerValueDto
    {
        /// MCQ_SINGLE | MCQ_MULTI | TRUE_FALSE | SHORT_ANSWER | ESSAY
        public string? Type { get; set; }

        /// Per-attempt AttemptQuestionOption.public_id values. Option-based types only.
        /// An empty array on MCQ_MULTI clears the answer.
        public List<Guid>? OptionIds { get; set; }

        /// SHORT_ANSWER / ESSAY only. An empty string clears the answer.
        public string? Text { get; set; }
    }
}
