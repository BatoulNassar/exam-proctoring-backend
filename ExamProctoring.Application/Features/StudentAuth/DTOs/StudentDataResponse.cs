namespace ExamProctoring.Application.Features.StudentAuth.DTOs
{
    /// Non-sensitive student identity returned to the desktop client after login.
    /// Deliberately excludes password, face_id, phone number and audit fields.
    public class StudentDataResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UniversityNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
