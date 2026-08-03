namespace ExamProctoring.Application.Common.Settings
{
    /// Failed-login and lockout policy for the student desktop client.
    /// Bound from the "StudentAuthentication" configuration section.
    public class StudentAuthenticationSettings
    {
        public int MaxFailedLoginAttempts { get; set; }

        public int LockoutDurationMinutes { get; set; }
    }
}
