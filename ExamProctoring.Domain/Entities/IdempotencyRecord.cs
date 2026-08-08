using ExamProctoring.Domain.Common;
using System;

namespace ExamProctoring.Domain.Entities
{
    /// Durable record of one completed idempotent mutation, so a client retry replays the
    /// original response instead of applying the change twice.
    ///
    /// Persisted in SQL Server rather than a cache because it must survive an API restart, an
    /// IIS recycle, a process crash, and must be shared across backend instances. Rows are
    /// never soft-deleted: the unique index is the concurrency control, and the global
    /// soft-delete filter would hide a row from the lookup while the database still enforced it.
    public class IdempotencyRecord : BaseEntity
    {
        /// The attempt the key is scoped to. A key is only meaningful within one attempt.
        public int student_session_id { get; set; }

        /// Client-supplied Idempotency-Key header value.
        public string idempotency_key { get; set; } = string.Empty;

        /// Which operation the key was used for, e.g. "PUT_ANSWER".
        public string endpoint { get; set; } = string.Empty;

        /// The resource the mutation targeted - the attempt question's public id for an answer
        /// write. Kept so reusing one key against a different question is diagnosable, not just
        /// detectable through the hash.
        public string resource_key { get; set; } = string.Empty;

        /// SHA-256 over the canonicalised semantic request. Two requests that mean the same
        /// thing hash the same even if their JSON differs; a genuinely different request does
        /// not, which is what turns a reused key into a conflict.
        public byte[] request_hash { get; set; } = Array.Empty<byte>();

        /// Frozen original response, replayed verbatim on retry.
        public int response_status { get; set; }
        public string response_body { get; set; } = string.Empty;

        public DateTime created_at_utc { get; set; }
    }
}
