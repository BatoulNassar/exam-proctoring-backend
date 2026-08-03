using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Common.Settings;
using ExamProctoring.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
        string GenerateRefreshToken();

        /// Issues an access token for a student desktop client. Carries stable student identity claims and
        /// the calling installation's device id only: no dashboard roles, no permissions and no exam context.
        (string AccessToken, DateTime ExpiresAtUtc) GenerateStudentAccessToken(Student student, string deviceId);
    }
}