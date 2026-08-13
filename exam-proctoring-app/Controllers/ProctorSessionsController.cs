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
    [Route("api/proctor-sessions")]
    [ApiController]
    [Authorize(Roles = "Proctor")]
    public class ProctorSessionsController : ControllerBase
    {
        private readonly IProctorSessionRepository _proctorSessionRepository;

        public ProctorSessionsController(IProctorSessionRepository proctorSessionRepository)
        {
            _proctorSessionRepository = proctorSessionRepository;
        }

        private int? GetActorId()
        {
            var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(actorIdClaim, out var actorId) ? actorId : null;
        }

        /// <summary>
        /// Get all sessions assigned to the logged-in proctor, paginated.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProctorSessions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var proctorId = GetActorId();
            if (proctorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var (sessions, totalCount) = await _proctorSessionRepository.GetProctorSessionsPagedAsync(proctorId.Value, page, pageSize);

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
