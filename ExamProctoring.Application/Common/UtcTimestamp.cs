using System;
using System.Globalization;

namespace ExamProctoring.Application.Common
{
    /// Single definition of how the student desktop client's ISO-8601 timestamps are accepted.
    /// Extracted from the device-check validator so every student endpoint applies the same
    /// rule rather than growing its own parser.
    public static class UtcTimestamp
    {
        /// Accepts only values carrying an explicit UTC designator - a trailing Z or a zero
        /// offset. A value without timezone information is rejected rather than assumed
        /// local, so the result never depends on the server's own time zone.
        public static bool TryParse(string? value, out DateTime utcValue)
        {
            utcValue = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var text = value.Trim();

            var hasUtcDesignator = text.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
                                   || text.EndsWith("+00:00", StringComparison.Ordinal)
                                   || text.EndsWith("-00:00", StringComparison.Ordinal);

            if (!hasUtcDesignator)
                return false;

            if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var parsed))
                return false;

            if (parsed.Offset != TimeSpan.Zero)
                return false;

            utcValue = parsed.UtcDateTime;
            return true;
        }

        /// Values read out of datetime2 columns come back with DateTimeKind.Unspecified; they
        /// are stamped as UTC here so responses serialize with a trailing 'Z'.
        public static DateTime AsUtc(DateTime value) =>
            value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
