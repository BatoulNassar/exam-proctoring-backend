using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Students.DTOs;

namespace ExamProctoring.Application.Features.Students.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;


        public StudentService(IStudentRepository studentRepository)
        {
            _studentRepository = studentRepository;
        }


        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _studentRepository.GetAllAsync();


            return students.Select(s => new StudentDto
            {
                Id = s.id,
                UserName = s.user_name,
                Email = s.email,
                PhoneNumber = s.phone_number,
                FirstName = s.first_name,
                MiddleName = s.middle_name,
                LastName = s.last_name,
                UniversityNumber = s.university_number
            });
        }
    }
}