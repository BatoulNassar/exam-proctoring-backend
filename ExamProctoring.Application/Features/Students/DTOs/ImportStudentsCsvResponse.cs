namespace ExamProctoring.Application.Features.Students.DTOs
{
    public class ImportStudentsCsvResponse
    {
        public int TotalRecords { get; set; }
        public int SuccessfulImports { get; set; }
        public int FailedImports { get; set; }
        public List<StudentImportResult> Results { get; set; } = new();
    }

    public class StudentImportResult
    {
        public int? StudentId { get; set; }
        public string UniversityNumber { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PhotoUrl { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
