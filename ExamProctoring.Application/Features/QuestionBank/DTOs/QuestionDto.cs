namespace ExamProctoring.Application.Features.QuestionBank.DTOs
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string QuestionText { get; set; }
        public int Marks { get; set; }
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? OptionE { get; set; }
        public string CorrectAnswer { get; set; }
    }
}
