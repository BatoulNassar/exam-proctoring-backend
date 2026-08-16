namespace ExamProctoring.Application.Features.Monitoring.DTOs
{
    public class StudentMonitoringDto
    {
        public int StudentSessionId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public string Status { get; set; }
        public DateTime? LoginAt { get; set; }
        public int OpenAlertCount { get; set; }
        public string? LatestAlertType { get; set; }
        public string? PipelineStatus { get; set; }
        public DateTime? LastHeartbeatAtUtc { get; set; }
    }
}
