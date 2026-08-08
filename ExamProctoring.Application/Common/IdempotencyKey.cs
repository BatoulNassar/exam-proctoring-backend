using System;

namespace ExamProctoring.Application.Common
{
    /// What counts as a usable idempotency key, and what its canonical form is.
    ///
    /// The API layer reads the Idempotency-Key header - that is transport. Deciding whether the
    /// value is semantically usable, and normalising it so a client that varies the casing of its
    /// own key still hits the same record, are business rules and belong here.
    public static class IdempotencyKey
    {
        /// The contract specifies a UUID v4. An empty GUID is rejected as well as a malformed one:
        /// a client sending all-zeroes is not supplying a key, it is supplying a placeholder.
        public static bool TryNormalise(string? rawKey, out string key)
        {
            key = string.Empty;

            if (!Guid.TryParse(rawKey, out var parsed) || parsed == Guid.Empty)
                return false;

            key = parsed.ToString("D").ToLowerInvariant();
            return true;
        }
    }
}
