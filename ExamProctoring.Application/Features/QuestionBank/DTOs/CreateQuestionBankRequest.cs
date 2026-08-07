namespace ExamProctoring.Application.Features.QuestionBank.DTOs
{
    public class CreateQuestionBankRequest
    {
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public string Version { get; set; }
        public Stream CsvFileStream { get; set; }
        public bool? Randomization { get; set; }
        public bool? OptionShuffle { get; set; }
    }
}
