namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public enum UpdateExamSessionResult
    {
        Updated = 1,
        NotFound = 2,
        NotDraft = 3,
        QuestionBankNotFound = 4,
        InvalidSettings = 5,
    }
}
