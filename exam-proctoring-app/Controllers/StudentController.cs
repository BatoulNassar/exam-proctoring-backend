using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.Students.DTOs;
using ExamProctoring.Application.Features.Students.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProctoring.API.Controllers
{
    [ApiController]
    [Route("api/students")]
    [Authorize]
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
    }
}