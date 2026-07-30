namespace ExamProctoring.Application.Features.QuestionBank.DTOs
{
    public enum UploadQuestionBankResult
    {
        Success,
        InvalidFile,
        InvalidCsv,
        DuplicateTitle,
        InternalError
    }

    public enum GetQuestionBankResult
    {
        Success,
        NotFound
    }
}
