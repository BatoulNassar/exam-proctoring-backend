namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public enum CreateExamSessionResult
    {
        Created = 1,
        InvalidSettings = 2,
        StartTimeInPast = 3,
        QuestionBankNotFound = 4,
        InvalidCsv = 5,
        InvalidProctor = 6,
        ProctorNotAvailable = 7,
    }
}
