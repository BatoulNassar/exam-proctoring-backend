namespace ExamProctoring.Application.Features.Alerts.DTOs
{
    public class AlertSummaryDto
    {
        public int TotalAlerts { get; set; }
        public int CriticalAlerts { get; set; }
        public int WarningAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
