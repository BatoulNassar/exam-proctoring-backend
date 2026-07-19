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
    }
}