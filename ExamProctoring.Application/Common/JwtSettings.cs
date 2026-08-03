namespace ExamProctoring.Application.Common.Settings
{
    public class JwtSettings
    {
        public string SecretKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int AccessTokenExpirationMinutes { get; set; }
        public int RefreshTokenExpirationDays { get; set; }

        /// Lifetime of student desktop access tokens, in minutes. Deliberately separate from
        /// AccessTokenExpirationMinutes so dashboard token lifetime is unaffected.
        public int StudentAccessTokenExpirationMinutes { get; set; }
    }
}
