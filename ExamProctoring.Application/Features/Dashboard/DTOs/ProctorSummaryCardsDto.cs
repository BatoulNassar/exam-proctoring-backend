namespace ExamProctoring.Application.Features.Dashboard.DTOs
{
    /// <summary>
    /// The three cards on the proctor dashboard. Every number is limited to the
    /// exam sessions the signed-in proctor is assigned to.
    /// </summary>
    public class ProctorSummaryCardsDto
    {
        public int ActiveSessions { get; set; }
        public int OpenAlerts { get; set; }

        /// <summary>Alerts resolved today inside the proctor's own sessions.</summary>
        public int ResolvedAlertsToday { get; set; }
    }
}
