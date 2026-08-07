using System;

namespace ExamProctoring.Application.Features.Monitoring.DTOs
{
    /// <summary>
    /// A detection reported by the student desktop client. The client reports what it
    /// observed; the backend decides whether that becomes an alert and how severe it
    /// is. Nothing here identifies the student — that comes from the token.
    /// </summary>
    public class ReportMonitoringEventRequest
    {
        /// <summary>The attempt this happened in. Verified against the caller's token.</summary>
        public int StudentSessionId { get; set; }

        /// <summary>Must be one of the codes from the alert type catalogue.</summary>
        public string EventType { get; set; }

        public string Details { get; set; }

        /// <summary>
        /// When the client observed it. Ignored if it lies in the future or is older
        /// than the session; the server clock is authoritative in those cases.
        /// </summary>
        public DateTime? OccurredAt { get; set; }
    }
}
