using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.QuestionBank.DTOs;
using ExamProctoring.Application.Features.QuestionBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace exam_proctoring_app.Controllers
{
    [Route("api/question-banks")]
    [ApiController]
    public class QuestionBankController : ControllerBase
    {
        private readonly IQuestionBankService _questionBankService;

        public QuestionBankController(IQuestionBankService questionBankService)
        {
            _questionBankService = questionBankService;
        }

        private int? GetActorId()
        {
            var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(actorIdClaim, out var actorId) ? actorId : null;
        }

        /// <summary>
        /// Upload a new question bank from CSV file.
        /// </summary>
        [HttpPost("upload")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadQuestionBank([FromForm] string title, [FromForm] string courseCode,
            [FromForm] string version, IFormFile csvFile, [FromForm] bool randomization, [FromForm] bool optionShuffle)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var request = new CreateQuestionBankRequest
            {
                Title = title,
                CourseCode = courseCode,
                Version = version,
                CsvFileStream = csvFile?.OpenReadStream(),
                Randomization = randomization,
                OptionShuffle = optionShuffle
            };

            var (result, data) = await _questionBankService.UploadQuestionBankAsync(request, actorId.Value);

            return result switch
            {
                UploadQuestionBankResult.InvalidFile => BadRequest(ApiResponse<object>.Fail("CSV file is required and must not be empty", 400)),
                UploadQuestionBankResult.InvalidCsv => BadRequest(ApiResponse<object>.Fail("CSV format is invalid or contains invalid data", 400)),
                UploadQuestionBankResult.DuplicateTitle => Conflict(ApiResponse<object>.Fail("A question bank with this title already exists", 409)),
                UploadQuestionBankResult.InternalError => StatusCode(500, ApiResponse<object>.Fail("Internal server error", 500)),
                _ => CreatedAtAction(
                    nameof(GetQuestionBankDetails),
                    new { id = data!.Id },
                    ApiResponse<QuestionBankDetailsDto>.Ok(data, "Question bank uploaded successfully", 201))
            };
        }

        /// <summary>
        /// Get all question banks with their status.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetAllQuestionBanks()
        {
            var banks = await _questionBankService.GetAllQuestionBanksAsync();
            return Ok(ApiResponse<IEnumerable<QuestionBankDto>>.Ok(banks, "Question banks retrieved successfully"));
        }

        /// <summary>
        /// Get detailed information of a specific question bank including all questions.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetQuestionBankDetails(int id)
        {
            var (result, data) = await _questionBankService.GetQuestionBankDetailsAsync(id);

            return result switch
            {
                GetQuestionBankResult.NotFound => NotFound(ApiResponse<object>.Fail("Question bank not found", 404)),
                _ => Ok(ApiResponse<QuestionBankDetailsDto>.Ok(data, "Question bank details retrieved successfully"))
            };
        }
    }
}
