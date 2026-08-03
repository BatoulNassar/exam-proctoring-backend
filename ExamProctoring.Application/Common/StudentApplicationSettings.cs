namespace ExamProctoring.Application.Common.Settings
{
    /// Client-application policy for the student desktop app.
    /// Bound from the "StudentApplication" configuration section.
    public class StudentApplicationSettings
    {
        /// Oldest Flutter build allowed to sign in, as major.minor.patch[+build].
        public string MinimumSupportedVersion { get; set; } = string.Empty;
    }
}
