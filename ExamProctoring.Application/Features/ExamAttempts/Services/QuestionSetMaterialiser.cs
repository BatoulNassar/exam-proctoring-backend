using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    /// Builds one student's personalised paper. Pure: no database, no clock, no randomness
    /// beyond the supplied seed, so it is fully reproducible and testable in isolation.
    ///
    /// The rows this produces are written once at first start and are thereafter the
    /// authoritative resume state - this class is never re-run for an attempt that already
    /// has a materialised paper. The seeded shuffle exists so a materialisation can be
    /// reproduced and reasoned about, not because ordering is recomputed on read.
    public static class QuestionSetMaterialiser
    {
        /// Slot letters in their natural authored order. Also the order options are emitted in
        /// when option shuffling is disabled.
        private static readonly string[] SlotOrder = { "a", "b", "c", "d", "e" };

        public sealed class MaterialisedOption
        {
            public int Ordinal { get; init; }
            public string SourceSlot { get; init; } = string.Empty;
            public string Label { get; init; } = string.Empty;
        }

        public sealed class MaterialisedQuestion
        {
            public int QuestionId { get; init; }
            public int Ordinal { get; init; }
            public QuestionType Type { get; init; }
            public string Stem { get; init; } = string.Empty;
            public int Marks { get; init; }
            public IReadOnlyList<MaterialisedOption> Options { get; init; } = Array.Empty<MaterialisedOption>();
        }

        /// Stable per-(student, session) seed, so the same pair always produces the same paper.
        public static int ComputeSeed(int studentId, int examSessionId) =>
            unchecked(studentId * 1_000_003 + examSessionId * 31 + 0x5F37);

        /// <param name="source">Authored questions. Never contains answer-key data.</param>
        /// <param name="randomizeQuestions">QuestionBank.randomization.</param>
        /// <param name="shuffleOptions">QuestionBank.option_shuffle.</param>
        /// <param name="seed">From <see cref="ComputeSeed"/>.</param>
        public static IReadOnlyList<MaterialisedQuestion> Build(
            IReadOnlyList<QuestionSourceView> source,
            bool randomizeQuestions,
            bool shuffleOptions,
            int seed)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Sorted explicitly rather than trusting the caller's ordering, so the result never
            // depends on how the database happened to return the rows.
            var ordered = source.OrderBy(q => q.QuestionId).ToList();

            if (randomizeQuestions)
                Shuffle(ordered, new DeterministicRandom(seed));

            var result = new List<MaterialisedQuestion>(ordered.Count);

            for (var i = 0; i < ordered.Count; i++)
            {
                var question = ordered[i];

                result.Add(new MaterialisedQuestion
                {
                    QuestionId = question.QuestionId,
                    Ordinal = i + 1,
                    Type = question.Type,
                    Stem = question.QuestionText ?? string.Empty,
                    Marks = question.Marks,
                    Options = BuildOptions(question, shuffleOptions, seed),
                });
            }

            return result;
        }

        private static IReadOnlyList<MaterialisedOption> BuildOptions(
            QuestionSourceView question, bool shuffleOptions, int seed)
        {
            // Free-text types are served with an empty option list, per the contract.
            if (!QuestionTypeMap.UsesOptions(question.Type))
                return Array.Empty<MaterialisedOption>();

            var slots = new List<(string Slot, string Label)>(SlotOrder.Length);

            foreach (var (slot, value) in EnumerateSlots(question))
            {
                if (!string.IsNullOrWhiteSpace(value))
                    slots.Add((slot, value.Trim()));
            }

            if (shuffleOptions)
            {
                // Sub-seeded per question so one question's option order does not depend on
                // where that question landed in the paper.
                Shuffle(slots, new DeterministicRandom(OptionSeed(seed, question.QuestionId)));
            }

            return slots
                .Select((entry, index) => new MaterialisedOption
                {
                    Ordinal = index + 1,
                    SourceSlot = entry.Slot,
                    Label = entry.Label,
                })
                .ToList();
        }

        /// Stable per-question option seed derived from the paper seed.
        private static int OptionSeed(int seed, int questionId) =>
            unchecked(seed * 31 + questionId * 397);

        private static IEnumerable<(string Slot, string? Value)> EnumerateSlots(QuestionSourceView q)
        {
            yield return (SlotOrder[0], q.OptionA);
            yield return (SlotOrder[1], q.OptionB);
            yield return (SlotOrder[2], q.OptionC);
            yield return (SlotOrder[3], q.OptionD);
            yield return (SlotOrder[4], q.OptionE);
        }

        /// In-place Fisher-Yates.
        private static void Shuffle<T>(IList<T> items, DeterministicRandom random)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        /// xorshift32. Deliberately not System.Random: this must produce the same sequence for
        /// a given seed across processes and .NET versions, which Random's algorithm does not
        /// guarantee.
        private sealed class DeterministicRandom
        {
            private uint _state;

            public DeterministicRandom(int seed)
            {
                // xorshift cannot recover from a zero state.
                _state = unchecked((uint)seed);
                if (_state == 0) _state = 0x9E3779B9;
            }

            /// Uniform in [0, exclusiveUpperBound).
            public int Next(int exclusiveUpperBound)
            {
                if (exclusiveUpperBound <= 1) return 0;

                // Rejection sampling removes the modulo bias that would otherwise skew the
                // first few positions of every shuffle.
                var limit = uint.MaxValue - (uint.MaxValue % (uint)exclusiveUpperBound);

                uint value;
                do { value = NextUInt(); } while (value >= limit);

                return (int)(value % (uint)exclusiveUpperBound);
            }

            private uint NextUInt()
            {
                var x = _state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                _state = x;
                return x;
            }
        }
    }
}
