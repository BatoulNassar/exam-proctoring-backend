using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    /// Builds the human-readable submission receipt: RCP-{courseTag}-{8 random characters}.
    /// Pure apart from the cryptographic RNG.
    ///
    /// Deliberately carries no personal data. The contract's own example embedded a student
    /// number, but a receipt gets screenshotted and read aloud to support, which would turn it
    /// into a PII leak for no benefit - the student identifies themselves anyway. The course tag
    /// stays because it genuinely helps triage and is not personal.
    ///
    /// The code is an identifier for humans only: it is never accepted as a route parameter and
    /// never functions as a credential.
    public static class ReceiptCodeGenerator
    {
        public const string Prefix = "RCP";

        /// Crockford base32: no I, L, O or U, so the code cannot be misread over the phone and
        /// cannot accidentally spell anything.
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        private const int RandomLength = 8;
        private const int MaxCourseTagLength = 12;

        /// <param name="courseTag">ExamSession.course_tag; sanitised, never trusted verbatim.</param>
        public static string Generate(string? courseTag)
        {
            var tag = SanitiseCourseTag(courseTag);
            var random = RandomSegment();

            return string.IsNullOrEmpty(tag)
                ? $"{Prefix}-{random}"
                : $"{Prefix}-{tag}-{random}";
        }

        /// Uppercase alphanumerics only and length-capped, so a course tag containing spaces,
        /// punctuation or unexpected length can never produce a malformed or oversized code.
        private static string SanitiseCourseTag(string? courseTag)
        {
            if (string.IsNullOrWhiteSpace(courseTag))
                return string.Empty;

            var cleaned = new string(courseTag
                .Trim()
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());

            return cleaned.Length > MaxCourseTagLength
                ? cleaned.Substring(0, MaxCourseTagLength)
                : cleaned;
        }

        /// RandomNumberGenerator rather than System.Random: this value is persisted under a
        /// unique constraint and shown to a student as proof of submission, so it must not come
        /// from a predictable sequence.
        private static string RandomSegment()
        {
            var builder = new StringBuilder(RandomLength);

            // Rejection sampling keeps the distribution uniform by discarding bytes above the
            // largest whole multiple of the alphabet size.
            //
            // Deliberately an int, not a byte: the alphabet is 32 characters, which divides 256
            // exactly, so the cutoff is 256 - 0 = 256. Narrowing that to a byte wraps it to 0,
            // every byte then compares as "out of range", and the loop below never terminates.
            var limit = 256 - (256 % Alphabet.Length);

            while (builder.Length < RandomLength)
            {
                var buffer = RandomNumberGenerator.GetBytes(RandomLength);

                foreach (var value in buffer)
                {
                    if (value >= limit)
                        continue;

                    builder.Append(Alphabet[value % Alphabet.Length]);

                    if (builder.Length == RandomLength)
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
