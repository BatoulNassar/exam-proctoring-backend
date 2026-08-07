using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;

namespace ExamProctoring.Domain.Entities
{
    /// One pre-exam device-readiness report submitted by the student desktop client.
    /// The measurements are client-reported and are never independently verified by the
    /// backend; this record is authenticated, attributable telemetry kept for diagnostics.
    /// Append-only: every submission is stored as a new attempt.
    public class DeviceCheck : BaseEntity
    {
        public int student_session_id { get; set; }

        /// Canonical "D" UUID taken from the authenticated token's device_id claim,
        /// never a hardware identifier.
        public string device_id { get; set; } = string.Empty;

        /// Client-reported instant the check ran. Informational only.
        public DateTime checked_at_utc { get; set; }

        /// Server instant the report was received. Authoritative for ordering.
        public DateTime received_at_utc { get; set; }

        /// The client's own proceed/block decision, stored exactly as reported and
        /// never recomputed or trusted by the backend.
        public bool client_can_proceed { get; set; }

        /// Exam session status at the moment the report was received.
        public ExamSessionStatus exam_session_status { get; set; }

        public StudentSession StudentSession { get; set; }
        public ICollection<DeviceCheckRequirement> Requirements { get; set; } = new List<DeviceCheckRequirement>();
    }
}
