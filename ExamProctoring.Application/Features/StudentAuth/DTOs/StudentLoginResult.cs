namespace ExamProctoring.Application.Features.StudentAuth.DTOs
{
    public enum StudentLoginStatus
    {
        Success,
        InvalidCredentials,
        AccountLocked,
        AccountInactive,
        AppVersionUnsupported,
    }

    /// Outcome of a student login attempt. The controller maps this to an HTTP status,
    /// a stable error code and the response envelope.
    public class StudentLoginResult
    {
        public StudentLoginStatus Status { get; private set; }

        public StudentLoginResponse? Response { get; private set; }

        /// Attempts left before lockout; set for <see cref="StudentLoginStatus.InvalidCredentials"/>.
        public int? RemainingAttempts { get; private set; }

        /// Seconds until the lockout expires; set for <see cref="StudentLoginStatus.AccountLocked"/>.
        public int? RetryAfterSeconds { get; private set; }

        /// Configured minimum client version; set for <see cref="StudentLoginStatus.AppVersionUnsupported"/>.
        public string? MinimumVersion { get; private set; }

        public static StudentLoginResult Success(StudentLoginResponse response) =>
            new() { Status = StudentLoginStatus.Success, Response = response };

        public static StudentLoginResult InvalidCredentials(int remainingAttempts) =>
            new() { Status = StudentLoginStatus.InvalidCredentials, RemainingAttempts = remainingAttempts };

        public static StudentLoginResult AccountLocked(int retryAfterSeconds) =>
            new() { Status = StudentLoginStatus.AccountLocked, RetryAfterSeconds = retryAfterSeconds };

        public static StudentLoginResult AccountInactive() =>
            new() { Status = StudentLoginStatus.AccountInactive };

        public static StudentLoginResult AppVersionUnsupported(string minimumVersion) =>
            new() { Status = StudentLoginStatus.AppVersionUnsupported, MinimumVersion = minimumVersion };
    }
}
