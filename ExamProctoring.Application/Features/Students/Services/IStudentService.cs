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
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
        Task<ImportStudentsCsvResponse> ImportStudentsFromCsvAsync(Stream csvStream);
    }
}
