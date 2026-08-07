namespace ExamProctoring.Application.Features.QuestionBank.DTOs
{
    public enum UploadQuestionBankResult
    {
        Success,
        InvalidFile,
        InvalidCsv,
        DuplicateCourseCode,
        InternalError
    }

    public enum GetQuestionBankResult
    {
        Success,
        NotFound
    }

    public enum DeleteQuestionBankResult
    {
        Success,
        NotFound,
        NotDraft
    }
}
