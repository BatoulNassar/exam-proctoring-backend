namespace ExamProctoring.Application.Features.QuestionBank.DTOs
{
    public class QuestionBankDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public string Status { get; set; }
        public string Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LockedAt { get; set; }
        public bool Randomization { get; set; }
        public bool OptionShuffle { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }
}
