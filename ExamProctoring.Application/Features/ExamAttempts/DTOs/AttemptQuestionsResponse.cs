using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// Response body for GET .../attempts/{attemptId}/questions - the full personalised set.
    ///
    /// SECURITY: there is deliberately no property anywhere in this file that could carry an
    /// answer key. correct_answer, the option source slot, and any scoring metadata are absent
    /// by construction rather than by remembering to omit them, and the read path never loads
    /// them from SQL Server in the first place.
    public class AttemptQuestionsResponse
    {
        public Guid AttemptId { get; set; }
        public int ExamSessionId { get; set; }
        public DateTime ServerTimeUtc { get; set; }
        public DateTime EndsAtUtc { get; set; }
        public List<AttemptQuestionDto> Questions { get; set; } = new();
    }

    public class AttemptQuestionDto
    {
        /// Opaque per-attempt question id. The authored Question.id is never exposed.
        public Guid QuestionId { get; set; }

        /// 1-based presentation order.
        public int Ordinal { get; set; }

        /// MCQ_SINGLE | MCQ_MULTI | TRUE_FALSE | SHORT_ANSWER | ESSAY
        public string Type { get; set; } = string.Empty;

        public string Stem { get; set; } = string.Empty;

        public int Marks { get; set; }

        /// Student-visible order. Empty for SHORT_ANSWER and ESSAY.
        public List<AttemptQuestionOptionDto> Options { get; set; } = new();

        /// The student's last accepted answer, or null if they have never saved one.
        /// Always emitted (never omitted) so the client can rely on the property existing.
        public SavedAnswerContentDto? SavedAnswer { get; set; }

        /// Omitted entirely for types with no contract-defined text limit.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AttemptQuestionConstraintsDto? Constraints { get; set; }
    }

    public class AttemptQuestionOptionDto
    {
        /// Opaque per-attempt option id, submitted back on an answer write.
        public Guid OptionId { get; set; }

        public string Label { get; set; } = string.Empty;
    }

    public class AttemptQuestionConstraintsDto
    {
        public int MaxLength { get; set; }
    }
}
