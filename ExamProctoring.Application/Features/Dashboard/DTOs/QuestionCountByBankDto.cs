namespace ExamProctoring.Application.Features.Dashboard.DTOs
{
    public class QuestionCountByBankDto
    {
        public int BankId { get; set; }
        public string CourseCode { get; set; }
        public string BankTitle { get; set; }
        public int QuestionCount { get; set; }
    }
}
