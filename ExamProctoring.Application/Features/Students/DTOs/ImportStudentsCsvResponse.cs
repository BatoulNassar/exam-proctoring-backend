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

        /// ENROLLED | NOT_ENROLLED — see <see cref="IdentityEnrollmentStatuses"/>.
        ///
        /// Every successfully imported student is ENROLLED: the backend generates the trusted
        /// reference embedding from the official photo, and a photo that cannot produce one
        /// fails that row rather than creating a student who would only discover the problem
        /// on exam day. NOT_ENROLLED therefore accompanies a failed row.
        public string IdentityEnrollment { get; set; } = IdentityEnrollmentStatuses.NotEnrolled;
    }

    /// Stable values for <see cref="StudentImportResult.IdentityEnrollment"/>.
    public static class IdentityEnrollmentStatuses
    {
        /// A trusted reference embedding was generated from the official photo and stored.
        public const string Enrolled = "ENROLLED";

        /// No reference is on file. Accompanies a row that failed enrolment; the message
        /// carries the reason.
        public const string NotEnrolled = "NOT_ENROLLED";
    }
}
