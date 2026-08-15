using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using System;

namespace ExamProctoring.Domain.Entities
{
    /// One submitted verification attempt. Append-only.
    ///
    /// The row is simultaneously the audit record and the idempotency record: the unique index
    /// on (identity_verification_session_id, client_attempt_id) is what stops a retried request
    /// from consuming a second attempt, and the stored outcome columns are what a replay
    /// returns. There is no frozen JSON body because every field of the response is a column
    /// here, so the replayed payload is reconstructed rather than cached.
    ///
    /// The 128-value probe embedding is deliberately NOT stored. It is biometric data with no
    /// use after the comparison, and keeping it would create a breach surface for no benefit.
    public class IdentityVerificationAttempt : BaseEntity
    {
        public int identity_verification_session_id { get; set; }

        /// Client-generated idempotency key. Unique within the verification session.
        public string client_attempt_id { get; set; } = string.Empty;

        public IdentityVerificationOutcome outcome { get; set; }

        /// 1-based position among attempts that consumed budget. For a non-consuming outcome
        /// (NO_ENROLLED_FACE) this carries the count at the time, so it never implies a
        /// consumption that did not happen.
        public int attempt_number { get; set; }

        /// What the response reported as attemptsRemaining. Stored so a replay is exact rather
        /// than recomputed against a counter that has since moved.
        public int attempts_remaining_after { get; set; }

        /// Cosine similarity against the trusted reference, 0..1. Null when no comparison ran
        /// (liveness rejected, or no enrolled face).
        public double? match_score { get; set; }

        /// The threshold in force when the decision was made. Kept so an outcome stays
        /// explainable after the threshold is recalibrated.
        public double? threshold_used { get; set; }

        // ----- client-reported liveness evidence, stored exactly as submitted -----
        // Never trusted; validated on arrival and retained so a forged payload is diagnosable.

        public bool liveness_accepted { get; set; }
        public int liveness_blink_count { get; set; }
        public int liveness_frames_analysed { get; set; }
        public int liveness_duration_ms { get; set; }
        public double liveness_min_eye_openness { get; set; }
        public double liveness_max_eye_openness { get; set; }

        /// Why the liveness evidence was refused. Null when it was accepted.
        public string? liveness_rejection_reason { get; set; }

        // ----- provenance -----

        public string embedding_model { get; set; } = string.Empty;
        public string embedding_model_version { get; set; } = string.Empty;

        /// The student's device clock, as submitted. Untrusted - audit only.
        public DateTime? captured_at_utc { get; set; }

        /// Server instant the attempt was processed. Authoritative for ordering.
        public DateTime attempted_at_utc { get; set; }

        public IdentityVerificationSession IdentityVerificationSession { get; set; } = null!;
    }
}
