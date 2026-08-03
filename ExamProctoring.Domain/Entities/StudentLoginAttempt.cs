using ExamProctoring.Domain.Common;
using System;

namespace ExamProctoring.Domain.Entities
{
    /// Failed-login state for a login identifier that does not resolve to a Student.
    /// Exists only so unknown identifiers produce the same attempt countdown and lockout
    /// as real accounts, preventing account enumeration.
    /// Stores a keyed hash of the normalized identifier - never the raw identifier,
    /// never a password or any other request content.
    public class StudentLoginAttempt : BaseEntity
    {
        public string identifier_hash { get; set; } = string.Empty;
        public int failed_attempts { get; set; }
        public DateTime? lockout_end_utc { get; set; }
    }
}
