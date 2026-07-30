namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public class EnrolledStudentDto
    {
        public int Id { get; set; }
        public string UniversityNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
