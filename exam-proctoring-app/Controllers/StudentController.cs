using ExamProctoring.API.Common;
using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Features.Students.DTOs;
using ExamProctoring.Application.Features.Students.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Route("api/students")]
    [Authorize(Policy = AuthorizationPolicies.DashboardOnly)]
    public class StudentsController : ControllerBase
    {

        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        private int? GetActorId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }


        [HttpGet]
        public async Task<IActionResult> GetAllStudents([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _studentService.GetAllStudentsAsync(page, pageSize);

            return Ok(ApiResponse<PagedResult<StudentDto>>.Ok(result, $"Retrieved {result.TotalCount} students"));
        }

        [HttpPost("import")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportStudents(IFormFile importZip)
        {
            if (importZip == null)
                return BadRequest(ApiResponse<object>.Fail("Import ZIP file is required", 400));

            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid user identity", 401));

            var response = await _studentService.ImportStudentsFromCsvAsync(
                importZip.OpenReadStream(), actorId.Value);

            return response.FailedImports > 0 && response.SuccessfulImports == 0
                ? BadRequest(ApiResponse<ImportStudentsCsvResponse>.Ok(response, "All records failed to import"))
                : Ok(ApiResponse<ImportStudentsCsvResponse>.Ok(response,
                    $"Import complete: {response.SuccessfulImports} succeeded, {response.FailedImports} failed"));
        }
    }
}