using ExamProctoring.Application.Features.QuestionBank.DTOs;

namespace ExamProctoring.Application.Features.QuestionBank.Services
{
    public interface IQuestionBankService
    {
        Task<(UploadQuestionBankResult Result, QuestionBankDetailsDto? Data)> UploadQuestionBankAsync(
            CreateQuestionBankRequest request, int adminId);

        Task<IEnumerable<QuestionBankDto>> GetAllQuestionBanksAsync();

        Task<(GetQuestionBankResult Result, QuestionBankDetailsDto? Data)> GetQuestionBankDetailsAsync(int id);
    }
}
