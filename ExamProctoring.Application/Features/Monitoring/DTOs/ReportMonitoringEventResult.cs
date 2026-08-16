namespace ExamProctoring.Application.Features.Monitoring.DTOs
{
    public enum ReportMonitoringEventOutcome
    {
        /// <summary>Recorded and an alert was raised for the proctors.</summary>
        AlertRaised = 1,

        /// <summary>Recorded, but did not meet the bar for an alert.</summary>
        RecordedOnly = 2,

        UnknownEventType = 3,
        SessionNotFound = 4,

        /// <summary>The attempt belongs to a different student.</summary>
        NotYourSession = 5,

        /// <summary>The attempt is not running, so there is nothing to monitor.</summary>
        SessionNotActive = 6,

        /// <summary>Snapshot payload exceeds the contract size cap.</summary>
        SnapshotTooLarge = 7,

        /// <summary>Missing/invalid fields (details, content type, base64).</summary>
        ValidationError = 8,

        /// <summary>Idempotency-Key was reused with a different request body.</summary>
        IdempotencyConflict = 9,
    }

    public class ReportMonitoringEventResult
    {
        public ReportMonitoringEventOutcome Outcome { get; set; }
        public int? MonitoringEventId { get; set; }
        public int? AlertId { get; set; }
        public string? SnapshotUrl { get; set; }
    }
}
