using System;
using System.Collections.Generic;

namespace ExamProctoring.Application.Features.Auth.DTOs
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }

        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }
}
