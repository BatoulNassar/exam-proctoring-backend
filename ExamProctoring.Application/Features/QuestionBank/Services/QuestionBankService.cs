using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.ExamAttempts;
using ExamProctoring.Application.Features.QuestionBank.DTOs;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using System.Text;

namespace ExamProctoring.Application.Features.QuestionBank.Services
{
    public class QuestionBankService : IQuestionBankService
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly ISystemSettingsRepository _settingsRepository;

        public QuestionBankService(IQuestionBankRepository questionBankRepository, ISystemSettingsRepository settingsRepository)
        {
            _questionBankRepository = questionBankRepository;
            _settingsRepository = settingsRepository;
        }

        public async Task<(UploadQuestionBankResult Result, QuestionBankDetailsDto? Data)> UploadQuestionBankAsync(
            CreateQuestionBankRequest request, int adminId)
        {
            if (request.CsvFileStream == null || request.CsvFileStream.Length == 0)
                return (UploadQuestionBankResult.InvalidFile, null);

            try
            {
                var questions = new List<Question>();

                using (var stream = new StreamReader(request.CsvFileStream, Encoding.UTF8))
                {
                    string line;
                    int lineNumber = 0;

                    while ((line = await stream.ReadLineAsync()) != null)
                    {
                        lineNumber++;

                        if (lineNumber == 1)
                            continue;

                        var parts = ParseCsvLine(line);

                        if (parts.Length < 9)
                            return (UploadQuestionBankResult.InvalidCsv, null);

                        var questionText = parts[0].Trim();
                        var typeStr = parts[1].Trim();
                        var marksStr = parts[2].Trim();
                        var optionA = parts[3].Trim();
                        var optionB = parts[4].Trim();
                        var optionC = parts[5].Trim();
                        var optionD = parts[6].Trim();
                        var optionE = parts.Length > 7 ? parts[7].Trim() : null;
                        var correctAnswer = parts[parts.Length - 1].Trim();

                        if (!int.TryParse(marksStr, out var marks) || marks <= 0)
                            return (UploadQuestionBankResult.InvalidCsv, null);

                        // Accepts both the internal enum names and the student API vocabulary
                        // (MCQ_SINGLE, MCQ_MULTI, TRUE_FALSE, SHORT_ANSWER, ESSAY), so an
                        // author can use whichever spelling they know. Replaces a bare
                        // Enum.TryParse, which could not express MCQ_MULTI or ESSAY.
                        if (!QuestionTypeMap.TryFromContract(typeStr, out var type))
                            return (UploadQuestionBankResult.InvalidCsv, null);

                        var question = new Question
                        {
                            question_text = questionText,
                            type = type,
                            marks = marks,
                            option_a = optionA,
                            option_b = optionB,
                            option_c = optionC,
                            option_d = optionD,
                            option_e = optionE,
                            correct_answer = correctAnswer,
                            created_at = DateTime.UtcNow,
                            created_by = adminId
                        };

                        questions.Add(question);
                    }
                }

                if (questions.Count == 0)
                    return (UploadQuestionBankResult.InvalidCsv, null);

                var existing = await _questionBankRepository.GetActiveByCourseCodeAsync(request.CourseCode.Trim());
                if (existing != null)
                    return (UploadQuestionBankResult.DuplicateCourseCode, null);

                var systemSettings = await _settingsRepository.GetAsync();
                var questionBank = new Domain.Entities.QuestionBank
                {
                    title = request.Title.Trim(),
                    course_code = request.CourseCode.Trim(),
                    version = request.Version.Trim(),
                    status = QuestionBankStatus.Draft,
                    randomization = request.Randomization ?? systemSettings?.question_randomisation ?? false,
                    option_shuffle = request.OptionShuffle ?? systemSettings?.option_shuffle ?? false,
                    authored_by_admin_id = adminId,
                    created_at = DateTime.UtcNow,
                    created_by = adminId,
                    Questions = questions
                };

                await _questionBankRepository.AddAsync(questionBank);

                var dto = MapToDetailsDto(questionBank);
                return (UploadQuestionBankResult.Success, dto);
            }
            catch (Exception)
            {
                return (UploadQuestionBankResult.InternalError, null);
            }
        }

        public async Task<IEnumerable<QuestionBankDto>> GetAllQuestionBanksAsync()
        {
            var banks = await _questionBankRepository.GetAllAsync();

            return banks.Select(b => new QuestionBankDto
            {
                Id = b.id,
                Title = b.title,
                CourseCode = b.course_code,
                Status = b.status.ToString(),
                Version = b.version,
                CreatedAt = b.created_at,
                LockedAt = b.locked_at,
                QuestionCount = b.Questions.Count
            });
        }

        public async Task<(GetQuestionBankResult Result, QuestionBankDetailsDto? Data)> GetQuestionBankDetailsAsync(int id)
        {
            var bank = await _questionBankRepository.GetByIdAsync(id);

            if (bank == null)
                return (GetQuestionBankResult.NotFound, null);

            var dto = MapToDetailsDto(bank);
            return (GetQuestionBankResult.Success, dto);
        }

        public async Task<DeleteQuestionBankResult> DeleteQuestionBankAsync(int id, int adminId)
        {
            var bank = await _questionBankRepository.GetByIdAsync(id);
            if (bank == null)
                return DeleteQuestionBankResult.NotFound;

            if (bank.status != QuestionBankStatus.Draft)
                return DeleteQuestionBankResult.NotDraft;

            bank.is_deleted = true;
            bank.deleted_at = DateTime.UtcNow;
            bank.deleted_by = adminId;

            await _questionBankRepository.UpdateAsync(bank);
            return DeleteQuestionBankResult.Success;
        }

        private QuestionBankDetailsDto MapToDetailsDto(Domain.Entities.QuestionBank bank)
        {
            return new QuestionBankDetailsDto
            {
                Id = bank.id,
                Title = bank.title,
                CourseCode = bank.course_code,
                Status = bank.status.ToString(),
                Version = bank.version,
                CreatedAt = bank.created_at,
                LockedAt = bank.locked_at,
                Randomization = bank.randomization,
                OptionShuffle = bank.option_shuffle,
                Questions = bank.Questions.Select(q => new QuestionDto
                {
                    Id = q.id,
                    Type = q.type.ToString(),
                    // Option-based types are mechanically gradable; the free-text ones are not.
                    // Display label only - no grading is implemented in this phase.
                    GradingMode = QuestionTypeMap.UsesOptions(q.type) ? "Auto" : "Manual",
                    QuestionText = q.question_text,
                    Marks = q.marks,
                    OptionA = q.option_a,
                    OptionB = q.option_b,
                    OptionC = q.option_c,
                    OptionD = q.option_d,
                    OptionE = q.option_e,
                    CorrectAnswer = q.correct_answer
                }).ToList()
            };
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
