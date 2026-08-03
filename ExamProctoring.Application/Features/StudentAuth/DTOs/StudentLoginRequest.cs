namespace ExamProctoring.Application.Features.StudentAuth.DTOs
{
    /// Login request sent by the Flutter Windows student desktop application.
    public class StudentLoginRequest
    {
        /// Student email (when it contains '@') or student username. Never a university number or phone number.
        public string? Identifier { get; set; }

        public string? Password { get; set; }

        /// UUID generated once by the desktop installation and reused on later logins.
        /// Not a hardware identifier; the backend never generates or discovers it.
        public string? DeviceId { get; set; }

        /// Flutter application version as major.minor.patch[+build], for example 1.0.0+1.
        public string? AppVersion { get; set; }
    }
}
