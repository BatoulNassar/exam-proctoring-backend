using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Features.Students.DTOs;
using ExamProctoring.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Students.Services
{
    public interface IStudentService
    {
        Task<PagedResult<StudentDto>> GetAllStudentsAsync(int page, int pageSize);
        Task<ImportStudentsCsvResponse> ImportStudentsFromCsvAsync(Stream importZipStream, int adminId);
    }
}
