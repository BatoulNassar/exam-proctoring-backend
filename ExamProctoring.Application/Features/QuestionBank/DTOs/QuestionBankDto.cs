namespace ExamProctoring.Application.Features.QuestionBank.DTOs
{
    public class QuestionBankDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public string Status { get; set; }
        public string Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LockedAt { get; set; }
        public int QuestionCount { get; set; }
    }
}
