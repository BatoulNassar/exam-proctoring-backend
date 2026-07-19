using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.ExamSessions.DTOs;
using ExamProctoring.Application.Features.ExamSessions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Route("api/exam-sessions")]
    public class ExamSessionsController : ControllerBase
    {
        private readonly IExamSessionService _examSessionService;

        public ExamSessionsController(IExamSessionService examSessionService)
        {
            _examSessionService = examSessionService;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetAllSessionsPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 4)
        {
            var sessions = await _examSessionService.GetAllSessionsAsync(page, pageSize);
            return Ok(ApiResponse<IEnumerable<ExamSessionDto>>.Ok(sessions, "The sessions were fetched successfully"));
        }

        /// <summary>
        /// Returns weekly exam session statistics.
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("weekly-statistics")]
        public async Task<IActionResult> GetWeeklyStatistics()
        {
            var result = await _examSessionService.GetWeeklyStatisticsAsync();

            return Ok(ApiResponse<IEnumerable<WeeklyExamSessionStatsDto>>.Ok(result, "Weekly statistics retrieved successfully"));
        }


    }
}