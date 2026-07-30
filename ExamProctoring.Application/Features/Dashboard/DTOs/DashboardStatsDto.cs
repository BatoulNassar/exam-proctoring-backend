namespace ExamProctoring.Application.Features.Dashboard.DTOs
{
    public class DashboardStatsDto
    {
        public int ActiveSessions { get; set; }
        public int StudentsInExam { get; set; }
        public int OpenAlerts { get; set; }
        public int QuestionBanks { get; set; }
        public int AdminUsers { get; set; }
    }
}
