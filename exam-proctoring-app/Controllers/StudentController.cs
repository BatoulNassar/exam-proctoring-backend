using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Students.DTOs;
using ExamProctoring.Application.Features.Students.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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


        [HttpGet]
        public async Task<IActionResult> GetAllStudents([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _studentService.GetAllStudentsAsync();

            return Ok(ApiResponse<IEnumerable<StudentDto>>.Ok(result, "Students retrieved successfully") );
        }

        [HttpPost("import-csv")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportStudentsCsv(IFormFile csvFile)
        {
            if (csvFile == null)
                return BadRequest(ApiResponse<object>.Fail("CSV file is required", 400));

            var response = await _studentService.ImportStudentsFromCsvAsync(csvFile.OpenReadStream());
            return response.FailedImports > 0 && response.SuccessfulImports == 0
                ? BadRequest(ApiResponse<ImportStudentsCsvResponse>.Fail("Import failed", 400))
                : Ok(ApiResponse<ImportStudentsCsvResponse>.Ok(response,
                    $"Imported: {response.SuccessfulImports} succeeded, {response.FailedImports} failed"));
        }
    }
}