using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    /// The one canonical server-side representation of a submitted answer, shared by all five
    /// question types. Pure: no database, no clock.
    ///
    /// Exactly one encoding exists so that what GET questions decodes is always what PUT
    /// encoded. The stored document carries only student-submitted semantic data - never an
    /// answer key, never a storage slot letter, never a grading result.
    public static class AnswerValueCodec
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };

        /// Storage shape. Option ids are per-attempt AttemptQuestionOption.public_id values.
        private sealed class StoredAnswer
        {
            public string Type { get; set; } = string.Empty;
            public List<Guid>? OptionIds { get; set; }
            public string? Text { get; set; }
        }

        /// Encodes an already-validated answer. Option ids keep the order the student submitted
        /// them in; only the idempotency hash treats them as an unordered set.
        public static string Encode(QuestionType type, IReadOnlyList<Guid> optionIds, string? text)
        {
            var stored = new StoredAnswer { Type = QuestionTypeMap.ToContract(type) };

            if (QuestionTypeMap.UsesOptions(type))
                stored.OptionIds = optionIds.ToList();
            else
                stored.Text = text ?? string.Empty;

            return JsonSerializer.Serialize(stored, Options);
        }

        /// Decodes a stored answer back into the student-facing shape.
        /// Returns null for content that cannot be read rather than throwing, so one damaged
        /// row cannot make a student's whole paper unloadable.
        public static AnswerValueDto? Decode(string? storedResponse)
        {
            if (string.IsNullOrWhiteSpace(storedResponse))
                return null;

            try
            {
                var stored = JsonSerializer.Deserialize<StoredAnswer>(storedResponse, Options);

                if (stored == null || string.IsNullOrWhiteSpace(stored.Type))
                    return null;

                if (!QuestionTypeMap.TryFromContract(stored.Type, out var type))
                    return null;

                return QuestionTypeMap.UsesOptions(type)
                    ? new AnswerValueDto { Type = stored.Type, OptionIds = stored.OptionIds ?? new List<Guid>() }
                    : new AnswerValueDto { Type = stored.Type, Text = stored.Text ?? string.Empty };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// True when the stored answer represents a deliberate clear (empty selection or empty
        /// text) rather than actual content. A cleared answer still keeps its row, so "cleared"
        /// stays distinguishable from "never answered", which is a null savedAnswer.
        public static bool IsCleared(AnswerValueDto? value)
        {
            if (value == null)
                return true;

            return QuestionTypeMap.TryFromContract(value.Type, out var type) && QuestionTypeMap.UsesOptions(type)
                ? value.OptionIds == null || value.OptionIds.Count == 0
                : string.IsNullOrEmpty(value.Text);
        }
    }
}
