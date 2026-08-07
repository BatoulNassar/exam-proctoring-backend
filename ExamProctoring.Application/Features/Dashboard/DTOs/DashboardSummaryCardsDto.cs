namespace ExamProctoring.Application.Features.Dashboard.DTOs
{
    /// <summary>
    /// The six numbers shown on the admin dashboard cards.
    /// </summary>
    public class DashboardSummaryCardsDto
    {
        public int TotalSessions { get; set; }
        public int ActiveSessions { get; set; }
        public int RegisteredStudents { get; set; }
        public int QuestionBanks { get; set; }
        public int OpenAlerts { get; set; }
        public int CriticalOpenAlerts { get; set; }
        public int ReadyToExport { get; set; }
        public int EscalatedAlerts { get; set; }
    }
}
