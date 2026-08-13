using ExamProctoring.API.Common;
using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    [Route("api/proctors")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class ProctorsController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public ProctorsController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Get all proctors in the system, paginated. Includes session and alert counts.
        /// Both SuperAdmin and Admin can view this list.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllProctors([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (proctors, totalCount) = await _userRepository.GetAllProctorsPagedAsync(page, pageSize);

            var result = new PagedResult<dynamic>
            {
                Items = proctors,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<dynamic>>.Ok(result, $"Retrieved {totalCount} proctors"));
        }
    }
}
