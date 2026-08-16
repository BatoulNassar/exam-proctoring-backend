using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    /// The auto-grading rules. Pure: no database, no clock, no logging.
    ///
    /// Scores against the MATERIALISED paper, never the live bank. The authored key names
    /// option slots ("a".."e") or TRUE/FALSE; the student's saved answer names per-attempt
    /// option UUIDs. Those two vocabularies are joined here through AttemptQuestionOption -
    /// which is exactly why option shuffling is safe: two students who both picked "the third
    /// option shown" submitted different UUIDs, and both resolve to the slot they actually chose.
    ///
    /// All-or-nothing by design. SRS §6.2 defines no partial credit and this must not invent it.
    public static class AttemptAutoGrader
    {
        public static bool IsAutoGraded(QuestionType type) =>
            type is QuestionType.MultipleChoice
                 or QuestionType.MultipleChoiceMulti
                 or QuestionType.TrueFalse;

        public static bool IsMcq(QuestionType type) =>
            type is QuestionType.MultipleChoice or QuestionType.MultipleChoiceMulti;

        /// Scores one auto-graded question.
        ///
        /// Throws <see cref="UnresolvableAnswerKeyException"/> when the authored key cannot be
        /// mapped onto this student's options. That is deliberately a failure rather than a
        /// silent zero: a malformed key would otherwise freeze an undeserved 0 into a student's
        /// permanent record, and a frozen wrong mark is far worse than a delayed receipt that an
        /// administrator can fix and the student can retry.
        public static AutoGradeResult Grade(GradingQuestionSourceView question)
        {
            var selectedSlots = ResolveSelectedSlots(question);
            var correctSlots = ResolveCorrectSlots(question);

            // Answered means the student left a non-empty selection. A deliberately cleared
            // answer keeps its row but is not an answer.
            var answered = selectedSlots.Count > 0;

            if (!answered)
                return new AutoGradeResult
                {
                    Answered = false,
                    MarksAwarded = 0,
                    SelectedSlots = string.Empty,
                    CorrectSlots = Join(correctSlots),
                };

            // MCQ_SINGLE and TRUE_FALSE need exactly one selection. More than one is not a
            // near-miss to be part-credited - it is not a valid answer to that question.
            var correct = question.Type switch
            {
                QuestionType.MultipleChoice or QuestionType.TrueFalse =>
                    selectedSlots.Count == 1 && correctSlots.Count == 1
                        && selectedSlots.SetEquals(correctSlots),

                // Set equality: order is irrelevant, a missing option fails, an extra option fails.
                QuestionType.MultipleChoiceMulti =>
                    correctSlots.Count > 0 && selectedSlots.SetEquals(correctSlots),

                _ => false,
            };

            return new AutoGradeResult
            {
                Answered = true,
                MarksAwarded = correct ? question.Marks : 0,
                SelectedSlots = Join(selectedSlots),
                CorrectSlots = Join(correctSlots),
            };
        }

        /// True when a saved answer carries content, for any question type. Used for the manual
        /// answered/unanswered counts as well, where nothing is scored.
        public static bool HasNonEmptyAnswer(GradingQuestionSourceView question)
        {
            var decoded = AnswerValueCodec.Decode(question.StoredResponse);
            return !AnswerValueCodec.IsCleared(decoded);
        }

        /// The student's submitted option UUIDs mapped back to authored slots. Unknown ids are
        /// dropped rather than trusted: an id that is not on this question cannot be a selection
        /// of it, and answer validation already rejects those on write.
        private static HashSet<string> ResolveSelectedSlots(GradingQuestionSourceView question)
        {
            var slots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var decoded = AnswerValueCodec.Decode(question.StoredResponse);
            if (decoded?.OptionIds == null)
                return slots;

            foreach (var optionId in decoded.OptionIds)
            {
                var option = question.Options.FirstOrDefault(o => o.PublicId == optionId);
                if (option != null && !string.IsNullOrWhiteSpace(option.SourceSlot))
                    slots.Add(option.SourceSlot.Trim());
            }

            return slots;
        }

        /// Parses the authored key into slots. Accepts the two forms the bank actually uses:
        /// comma-separated slot letters ("A", "A,C"), and TRUE/FALSE, which names the option
        /// text rather than a slot and is therefore matched against the frozen option labels.
        private static HashSet<string> ResolveCorrectSlots(GradingQuestionSourceView question)
        {
            var slots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(question.CorrectAnswer))
                throw new UnresolvableAnswerKeyException(question.Ordinal, "the answer key is empty");

            var tokens = question.CorrectAnswer
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var token in tokens)
            {
                var option = question.Options.FirstOrDefault(o =>
                                 string.Equals(o.SourceSlot, token, StringComparison.OrdinalIgnoreCase))
                             ?? question.Options.FirstOrDefault(o =>
                                 string.Equals(o.Label?.Trim(), token, StringComparison.OrdinalIgnoreCase));

                if (option == null)
                    throw new UnresolvableAnswerKeyException(
                        question.Ordinal, "the answer key does not match any option on the paper");

                slots.Add(option.SourceSlot.Trim());
            }

            if (slots.Count == 0)
                throw new UnresolvableAnswerKeyException(question.Ordinal, "the answer key resolved to no option");

            return slots;
        }

        private static string Join(IEnumerable<string> slots) =>
            string.Join(",", slots.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                  .ToUpperInvariant();
    }

    public sealed class AutoGradeResult
    {
        public bool Answered { get; init; }
        public int MarksAwarded { get; init; }

        /// SERVER-ONLY diagnostics persisted on the AutoScore row.
        public string SelectedSlots { get; init; } = string.Empty;
        public string CorrectSlots { get; init; } = string.Empty;
    }

    /// The authored key could not be mapped onto the student's materialised options.
    ///
    /// The message deliberately names the ordinal and the shape of the problem but NEVER the
    /// key itself, so a broken bank question is diagnosable from the logs without the logs
    /// becoming a place the answer key leaks.
    public sealed class UnresolvableAnswerKeyException : Exception
    {
        public UnresolvableAnswerKeyException(int ordinal, string reason)
            : base($"Question at ordinal {ordinal} could not be auto-graded: {reason}.")
        {
            Ordinal = ordinal;
        }

        public int Ordinal { get; }
    }
}
