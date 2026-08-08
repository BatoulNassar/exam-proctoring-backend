using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.ExamAttempts;
using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using ExamProctoring.Application.Features.ExamAttempts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    /// The student's exam attempt lifecycle, called by the Flutter Windows client after identity
    /// verification has passed: start/resume, load the paper, save answers, submit.
    ///
    /// Student tokens only; dashboard tokens are rejected by the StudentOnly policy.
    ///
    /// Every action is transport only - bind, read claims, call one use case, map the result.
    /// Request validation is applied by ValidateRequest, unhandled exceptions by
    /// ApiExceptionFilter, and outcome-to-HTTP by ExamAttemptResultMapper. No decision about
    /// timers, devices, identity, answers, idempotency or finalisation is made in this file.
    [ApiController]
    [Route("api/v1/exam-sessions")]
    [Authorize(Policy = AuthorizationPolicies.StudentOnly)]
    [ValidateRequest]
    [ApiExceptionFilter(Code = ExamAttemptErrorCodes.ServerError)]
    public class ExamAttemptsController : ControllerBase
    {
        private const int MaxStartRequestBytes = 8 * 1024;

        /// Comfortably covers a 4000-character ESSAY plus JSON overhead.
        private const int MaxAnswerRequestBytes = 32 * 1024;

        private const string IdempotencyKeyHeader = "Idempotency-Key";

        private readonly IExamAttemptService _examAttemptService;

        public ExamAttemptsController(IExamAttemptService examAttemptService)
        {
            _examAttemptService = examAttemptService;
        }

        /// Creates the student's attempt, or resumes it after a crash or reconnect.
        /// 201 the first time, 200 for every later call with identical state.
        [HttpPost("{examSessionId:int}/attempts/start")]
        [RequestSizeLimit(MaxStartRequestBytes)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Start(int examSessionId, [FromBody] StartAttemptRequest request)
        {
            if (!User.TryGetStudentId(out var studentId))
                return ApiResults.AccountInactive();

            var result = await _examAttemptService.StartAsync(
                request, studentId, examSessionId, User.GetDeviceId());

            return ExamAttemptResultMapper.Map(result);
        }

        /// The full personalised question set for this attempt. Read-only, no paging for v1.
        [HttpGet("{examSessionId:int}/attempts/{attemptId:guid}/questions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetQuestions(int examSessionId, Guid attemptId)
        {
            if (!User.TryGetStudentId(out var studentId))
                return ApiResults.AccountInactive();

            var result = await _examAttemptService.GetQuestionsAsync(studentId, examSessionId, attemptId);

            return ExamAttemptResultMapper.Map(result);
        }

        /// Upserts the student's answer to one question. Idempotent on the Idempotency-Key header;
        /// the header is read here, but what a valid key means and how a replay behaves are decided
        /// in the Application layer.
        [HttpPut("{examSessionId:int}/attempts/{attemptId:guid}/answers/{questionId:guid}")]
        [RequestSizeLimit(MaxAnswerRequestBytes)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertAnswer(
            int examSessionId,
            Guid attemptId,
            Guid questionId,
            [FromBody] UpsertAnswerRequest request,
            [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey)
        {
            if (!User.TryGetStudentId(out var studentId))
                return ApiResults.AccountInactive();

            var result = await _examAttemptService.UpsertAnswerAsync(
                request, studentId, examSessionId, attemptId, questionId, idempotencyKey);

            return ExamAttemptResultMapper.Map(result);
        }

        /// Finalises the attempt. Always 200 once the attempt is terminal - whether this call
        /// finalised it, an earlier call did, a proctor terminated it, or the server expired it.
        [HttpPost("{examSessionId:int}/attempts/{attemptId:guid}/submit")]
        [RequestSizeLimit(MaxStartRequestBytes)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Submit(
            int examSessionId,
            Guid attemptId,
            [FromBody] SubmitAttemptRequest request,
            [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey)
        {
            if (!User.TryGetStudentId(out var studentId))
                return ApiResults.AccountInactive();

            var result = await _examAttemptService.SubmitAsync(
                request, studentId, examSessionId, attemptId, idempotencyKey);

            return ExamAttemptResultMapper.Map(result);
        }
    }
}
