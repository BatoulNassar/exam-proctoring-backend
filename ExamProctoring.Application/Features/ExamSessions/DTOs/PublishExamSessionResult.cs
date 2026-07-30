namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public enum PublishExamSessionResult
    {
        Published = 1,
        NotFound = 2,
        NotDraft = 3,
        StartTimeInPast = 4,
        QuestionBankNotReady = 5,
    }
}
