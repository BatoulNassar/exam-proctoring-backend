namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public class WeeklyExamSessionStatsDto
    {
        public string Day { get; set; }

        public int TotalSessions { get; set; }

        public int TotalStudents { get; set; }
    }
}