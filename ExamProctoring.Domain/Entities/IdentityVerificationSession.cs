using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;

namespace ExamProctoring.Domain.Entities
{
    /// One student's identity verification for one exam assignment (FR-01).
    ///
    /// Scoped to StudentSession rather than to the student, because identity is established
    /// per exam sitting: a pass for yesterday's exam must not admit anyone to today's. The
    /// unique index on student_session_id is what makes "create or resume" exact - there can
    /// only ever be one, so a client that crashes after two failed attempts reopens the same
    /// row rather than receiving a fresh budget.
    ///
    /// No expiry column: the contract defines none, and the exam session's own login window
    /// already bounds when verification is useful. Inventing a second clock here would create
    /// a way for a student to be refused for a reason the client has no code to explain.
    public class IdentityVerificationSession : BaseEntity
    {
        /// The exam assignment this verification belongs to. Unique - one per assignment.
        public int student_session_id { get; set; }

        /// Student-facing opaque identifier used in the route. The integer primary key is
        /// never exposed, so verification sessions cannot be enumerated.
        public Guid public_id { get; set; }

        public IdentityVerificationStatus status { get; set; }

        /// Completed attempts that consumed budget. Only ever incremented by a conditional
        /// UPDATE guarded on attempts_used &lt; max_attempts, so two concurrent attempts can
        /// never both pass the limit.
        public int attempts_used { get; set; }

        /// Frozen at creation from SystemSettings.max_liveness_attempts. Stored rather than
        /// read live so an admin editing the setting mid-exam cannot shrink a budget a student
        /// has already been shown, nor silently grant extra tries.
        public int max_attempts { get; set; }

        /// Server instant of the successful match. Null while pending. Its presence is the
        /// terminal marker and the guard that makes success a one-time transition.
        public DateTime? verified_at_utc { get; set; }

        /// Canonical "D" UUID of the device that created this verification session, taken from
        /// the authenticated token's device_id claim. Recorded for audit; it is not used to
        /// refuse attempts, because the contract defines no device-mismatch outcome here.
        public string? device_id { get; set; }

        public StudentSession StudentSession { get; set; } = null!;

        public ICollection<IdentityVerificationAttempt> Attempts { get; set; }
            = new List<IdentityVerificationAttempt>();
    }
}
