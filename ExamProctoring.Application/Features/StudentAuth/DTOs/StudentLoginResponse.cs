using System;

namespace ExamProctoring.Application.Features.StudentAuth.DTOs
{
    /// Payload carried inside ApiResponse&lt;T&gt;.Data on a successful student login.
    /// No refresh token is issued for the desktop client.
    public class StudentLoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        /// UTC expiry of the access token; matches the token's own 'exp' claim.
        public DateTime ExpiresAtUtc { get; set; }

        /// Server clock at the moment the response was produced, in UTC.
        public DateTime ServerTimeUtc { get; set; }

        public StudentDataResponse StudentData { get; set; } = new();
    }
}
