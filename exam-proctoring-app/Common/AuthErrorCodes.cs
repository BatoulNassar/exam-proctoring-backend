namespace ExamProctoring.API.Common
{
    /// Stable machine-readable authentication codes for the student desktop client.
    /// These values are part of the API contract and are never localized.
    public static class AuthErrorCodes
    {
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string AccountLocked = "ACCOUNT_LOCKED";
        public const string AccountInactive = "ACCOUNT_INACTIVE";
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string AppVersionUnsupported = "APP_VERSION_UNSUPPORTED";
        public const string MultipleActiveSessions = "MULTIPLE_ACTIVE_SESSIONS";
        public const string DeviceMismatch = "DEVICE_MISMATCH";
        public const string SessionNotFound = "SESSION_NOT_FOUND";
    }
}
