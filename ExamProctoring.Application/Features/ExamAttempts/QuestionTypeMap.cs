using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;

namespace ExamProctoring.Application.Features.ExamAttempts
{
    /// Translation between the persisted <see cref="QuestionType"/> names and the exact
    /// vocabulary the student API contract uses. The two differ on purpose: the enum members
    /// are persisted as strings and cannot be renamed without breaking existing Question rows,
    /// while the contract vocabulary is fixed by the Flutter client.
    public static class QuestionTypeMap
    {
        public const string McqSingle = "MCQ_SINGLE";
        public const string McqMulti = "MCQ_MULTI";
        public const string TrueFalse = "TRUE_FALSE";
        public const string ShortAnswer = "SHORT_ANSWER";
        public const string Essay = "ESSAY";

        /// Contract text length limits. Only the two free-text types have one; returning a
        /// limit for an option-based type would be inventing a constraint the contract does
        /// not define.
        public const int ShortAnswerMaxLength = 500;
        public const int EssayMaxLength = 4000;

        private static readonly Dictionary<QuestionType, string> ToContractMap = new()
        {
            { QuestionType.MultipleChoice,      McqSingle },
            { QuestionType.MultipleChoiceMulti, McqMulti },
            { QuestionType.TrueFalse,           TrueFalse },
            { QuestionType.ShortAnswer,         ShortAnswer },
            { QuestionType.Essay,               Essay },
        };

        /// Accepts both the contract vocabulary and the internal enum names, so an admin CSV
        /// may use either spelling.
        ///
        /// Built with TryAdd rather than a collection initializer because the two vocabularies
        /// overlap under case-insensitive comparison - "ESSAY" and nameof(Essay) are the same
        /// key - and a duplicate would throw while initializing the type, taking down every
        /// caller. Both spellings map to the same value anyway, so the first one wins.
        private static readonly Dictionary<string, QuestionType> FromContractMap = BuildFromContractMap();

        private static Dictionary<string, QuestionType> BuildFromContractMap()
        {
            var map = new Dictionary<string, QuestionType>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in ToContractMap)
            {
                map.TryAdd(pair.Value, pair.Key);              // contract vocabulary
                map.TryAdd(pair.Key.ToString(), pair.Key);     // internal enum name
            }

            return map;
        }

        /// The value the student API must emit. Throws rather than guessing if a new enum
        /// member is added without a contract mapping - a silent fallback here would ship an
        /// unrecognised type to the client.
        public static string ToContract(QuestionType type) =>
            ToContractMap.TryGetValue(type, out var value)
                ? value
                : throw new ArgumentOutOfRangeException(
                    nameof(type), type, "Question type has no student API contract mapping.");

        public static bool TryFromContract(string? value, out QuestionType type)
        {
            type = default;

            return !string.IsNullOrWhiteSpace(value)
                   && FromContractMap.TryGetValue(value.Trim(), out type);
        }

        /// Option-based types must be served with a non-empty option list; the free-text
        /// types must be served with an empty one.
        public static bool UsesOptions(QuestionType type) =>
            type is QuestionType.MultipleChoice or QuestionType.MultipleChoiceMulti or QuestionType.TrueFalse;

        /// Null when the type has no contract-defined text limit.
        public static int? TextMaxLength(QuestionType type) => type switch
        {
            QuestionType.ShortAnswer => ShortAnswerMaxLength,
            QuestionType.Essay => EssayMaxLength,
            _ => null,
        };
    }
}
