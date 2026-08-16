namespace ExamProctoring.Application.Features.Alerts.DTOs
{
    public class AlertEventDto
    {
        public int Id { get; set; }
        public string AlertType { get; set; }
        public string AlertDescription { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public int StudentId { get; set; }
        public int StudentSessionId { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public int SessionId { get; set; }
        public string SessionName { get; set; }
        public DateTime TriggeredAt { get; set; }
        public string? SnapshotUrl { get; set; }
        public string ActionTaken { get; set; }
        public string ActionNote { get; set; }
        public DateTime? ActionAt { get; set; }
    }
}
