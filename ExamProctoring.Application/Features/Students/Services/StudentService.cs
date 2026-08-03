using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Students.DTOs;
using ExamProctoring.Domain.Entities;
using System.Text;

namespace ExamProctoring.Application.Features.Students.Services
{
    public class StudentService : IStudentService
    {
        private const string DefaultImportPassword = "DefaultPassword123!";

        private readonly IStudentRepository _studentRepository;
        private readonly IPasswordHasher _passwordHasher;

        public StudentService(IStudentRepository studentRepository, IPasswordHasher passwordHasher)
        {
            _studentRepository = studentRepository;
            _passwordHasher = passwordHasher;
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

        public async Task<ImportStudentsCsvResponse> ImportStudentsFromCsvAsync(Stream csvStream)
        {
            var response = new ImportStudentsCsvResponse();

            if (csvStream == null || csvStream.Length == 0)
            {
                response.Results.Add(new StudentImportResult
                {
                    IsSuccess = false,
                    Message = "CSV file is empty or invalid"
                });
                return response;
            }

            try
            {
                using var stream = new StreamReader(csvStream, Encoding.UTF8);
                string line;
                int lineNumber = 0;

                while ((line = await stream.ReadLineAsync()) != null)
                {
                    lineNumber++;

                    if (lineNumber == 1)
                        continue;

                    var result = await ProcessCsvLine(line, lineNumber);
                    response.Results.Add(result);

                    response.TotalRecords++;
                    if (result.IsSuccess)
                        response.SuccessfulImports++;
                    else
                        response.FailedImports++;
                }
            }
            catch (Exception ex)
            {
                response.Results.Add(new StudentImportResult
                {
                    IsSuccess = false,
                    Message = $"Error reading CSV file: {ex.Message}"
                });
            }

            return response;
        }

        private async Task<StudentImportResult> ProcessCsvLine(string line, int lineNumber)
        {
            try
            {
                var parts = ParseCsvLine(line);

                if (parts.Length < 2)
                {
                    return new StudentImportResult
                    {
                        IsSuccess = false,
                        Message = $"Line {lineNumber}: Insufficient columns. Minimum required: university_number, full_name (or first_name, last_name)"
                    };
                }

                var universityNumber = parts[0].Trim();
                string firstName = string.Empty;
                string lastName = string.Empty;
                string email = string.Empty;
                string phoneNumber = string.Empty;
                string faceId = string.Empty;

                // Format: university_number,full_name,email,phone_number,face_id
                if (parts.Length >= 5)
                {
                    var fullName = parts[1].Trim().Split(' ');
                    firstName = fullName[0];
                    lastName = fullName.Length > 1 ? string.Join(" ", fullName.Skip(1)) : fullName[0];
                    email = parts[2].Trim();
                    phoneNumber = parts[3].Trim();
                    faceId = parts[4].Trim();
                }
                else if (parts.Length == 4)
                {
                    var fullName = parts[1].Trim().Split(' ');
                    firstName = fullName[0];
                    lastName = fullName.Length > 1 ? string.Join(" ", fullName.Skip(1)) : fullName[0];
                    email = parts[2].Trim();
                    phoneNumber = parts[3].Trim();
                }
                else if (parts.Length == 3)
                {
                    var fullName = parts[1].Trim().Split(' ');
                    firstName = fullName[0];
                    lastName = fullName.Length > 1 ? string.Join(" ", fullName.Skip(1)) : fullName[0];
                    email = parts[2].Trim();
                }
                else if (parts.Length == 2)
                {
                    var fullName = parts[1].Trim().Split(' ');
                    firstName = fullName[0];
                    lastName = fullName.Length > 1 ? string.Join(" ", fullName.Skip(1)) : fullName[0];
                }

                if (string.IsNullOrEmpty(universityNumber) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                {
                    return new StudentImportResult
                    {
                        UniversityNumber = universityNumber,
                        FullName = $"{firstName} {lastName}",
                        IsSuccess = false,
                        Message = $"Line {lineNumber}: University number, first name, and last name are required"
                    };
                }

                var existingStudent = await _studentRepository.GetByUniversityNumberAsync(universityNumber);

                if (existingStudent != null)
                {
                    existingStudent.first_name = firstName;
                    existingStudent.last_name = lastName;
                    if (!string.IsNullOrEmpty(email))
                        existingStudent.email = email;
                    if (!string.IsNullOrEmpty(phoneNumber))
                        existingStudent.phone_number = phoneNumber;
                    if (!string.IsNullOrEmpty(faceId))
                        existingStudent.face_id = faceId;
                    existingStudent.updated_at = DateTime.UtcNow;

                    await _studentRepository.UpdateAsync(existingStudent);

                    return new StudentImportResult
                    {
                        StudentId = existingStudent.id,
                        UniversityNumber = universityNumber,
                        FullName = $"{firstName} {lastName}",
                        Email = existingStudent.email,
                        PhoneNumber = existingStudent.phone_number,
                        FaceId = existingStudent.face_id,
                        IsSuccess = true,
                        Message = "Updated existing student"
                    };
                }
                else
                {
                    var generatedEmail = string.IsNullOrEmpty(email)
                        ? $"{universityNumber.Replace("-", "")}@student.vu.edu".ToLower()
                        : email;

                    var newStudent = new Student
                    {
                        university_number = universityNumber,
                        first_name = firstName,
                        last_name = lastName,
                        email = generatedEmail,
                        phone_number = phoneNumber,
                        user_name = universityNumber,
                        // Stored as a BCrypt hash; the student still signs in with the plaintext default.
                        password = _passwordHasher.Hash(DefaultImportPassword),
                        face_id = faceId ?? string.Empty,
                        created_at = DateTime.UtcNow
                    };

                    await _studentRepository.AddAsync(newStudent);

                    return new StudentImportResult
                    {
                        StudentId = newStudent.id,
                        UniversityNumber = universityNumber,
                        FullName = $"{firstName} {lastName}",
                        Email = generatedEmail,
                        PhoneNumber = phoneNumber,
                        FaceId = faceId,
                        IsSuccess = true,
                        Message = "Created new student"
                    };
                }
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return new StudentImportResult
                {
                    IsSuccess = false,
                    Message = $"Line {lineNumber}: {message}"
                };
            }
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}