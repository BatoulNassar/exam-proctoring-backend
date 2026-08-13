namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public class AdminSessionDto
    {
        public int SessionId { get; set; }
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public string Status { get; set; }
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalEnrolledStudents { get; set; }
        public int AssignedProctors { get; set; }
        public int OpenAlerts { get; set; }
    }
}
