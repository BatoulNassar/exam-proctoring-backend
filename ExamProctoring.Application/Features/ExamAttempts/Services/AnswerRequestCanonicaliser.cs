using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    /// Turns an answer write into a stable semantic fingerprint for idempotency comparison.
    /// Pure: no database, no clock, no randomness.
    ///
    /// Deliberately NOT a hash of the raw request bytes. Two requests that mean the same thing
    /// must fingerprint the same, otherwise a client that re-serialises its buffered payload
    /// slightly differently would get a spurious 409 on a legitimate retry. Conversely a
    /// genuinely different request must fingerprint differently, which is what makes a reused
    /// key detectable.
    ///
    /// Normalised away (cosmetic):
    ///   - JSON property order and whitespace, by projecting to a fixed field order
    ///   - option id order and letter case, by lower-casing and sorting
    ///   - surrounding text whitespace, by trimming
    ///
    /// Excluded entirely (audit-only, must not cause a conflict):
    ///   - clientAnsweredAtUtc, which a retrying client legitimately re-stamps
    ///   - clientMutationId
    ///
    /// Included (semantic): the target question, the answer type, the answer content, and the
    /// clamped durationMs that will actually be stored.
    public static class AnswerRequestCanonicaliser
    {
        /// Builds the canonical string. Exposed separately from the hash so a mismatch can be
        /// diagnosed by eye during development.
        public static string Canonicalise(
            Guid attemptQuestionPublicId,
            QuestionType type,
            IReadOnlyList<Guid> optionIds,
            string? text,
            int clampedDurationMs)
        {
            var builder = new StringBuilder();

            builder.Append("q=").Append(attemptQuestionPublicId.ToString("D").ToLowerInvariant()).Append('\n');
            builder.Append("type=").Append(QuestionTypeMap.ToContract(type)).Append('\n');

            if (QuestionTypeMap.UsesOptions(type))
            {
                // Sorted, so MCQ_MULTI selections that arrive in a different order are the same
                // request. For the single-selection types sorting is a no-op.
                var canonicalOptions = optionIds
                    .Select(id => id.ToString("D").ToLowerInvariant())
                    .OrderBy(id => id, StringComparer.Ordinal);

                builder.Append("options=").Append(string.Join(",", canonicalOptions)).Append('\n');
            }
            else
            {
                // Length-prefixed so text containing the delimiter cannot forge a different
                // field boundary and collide with another request.
                var normalisedText = (text ?? string.Empty).Trim();
                builder.Append("text:").Append(normalisedText.Length.ToString(CultureInfo.InvariantCulture))
                       .Append('=').Append(normalisedText).Append('\n');
            }

            builder.Append("durationMs=").Append(clampedDurationMs.ToString(CultureInfo.InvariantCulture)).Append('\n');

            return builder.ToString();
        }

        public static byte[] Hash(
            Guid attemptQuestionPublicId,
            QuestionType type,
            IReadOnlyList<Guid> optionIds,
            string? text,
            int clampedDurationMs) =>
            SHA256.HashData(Encoding.UTF8.GetBytes(
                Canonicalise(attemptQuestionPublicId, type, optionIds, text, clampedDurationMs)));

        /// Fixed-time comparison is unnecessary here (neither side is a secret), but an
        /// explicit helper keeps the intent obvious at the call site.
        public static bool HashesMatch(byte[]? left, byte[]? right) =>
            left != null && right != null && left.AsSpan().SequenceEqual(right);
    }
}
