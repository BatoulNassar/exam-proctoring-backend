using ExamProctoring.API.Common;
using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Features.ExamSessions.DTOs;
using ExamProctoring.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExamProctoring.API.Controllers
{
    [Route("api/admin-sessions")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminSessionsController : ControllerBase
    {
        private readonly IExamSessionRepository _examSessionRepository;

        public AdminSessionsController(IExamSessionRepository examSessionRepository)
        {
            _examSessionRepository = examSessionRepository;
        }

        private int? GetActorId()
        {
            var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(actorIdClaim, out var actorId) ? actorId : null;
        }

        /// <summary>
        /// Get all sessions created by the logged-in admin, paginated.
        /// SuperAdmin cannot use this endpoint — they see all sessions via /api/exam-sessions.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAdminSessions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var adminId = GetActorId();
            if (adminId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (sessions, totalCount) = await _examSessionRepository.GetAdminSessionsPagedAsync(adminId.Value, page, pageSize);

            var result = new PagedResult<dynamic>
            {
                Items = sessions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<dynamic>>.Ok(result, $"Retrieved {totalCount} sessions"));
        }
    }
}
