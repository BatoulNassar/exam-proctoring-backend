using ExamProctoring.Application.Common;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Common.Settings;
using ExamProctoring.Application.Features.StudentAuth.DTOs;
using ExamProctoring.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.StudentAuth.Services
{
    public class StudentAuthService : IStudentAuthService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IStudentLoginAttemptRepository _loginAttemptRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly StudentAuthenticationSettings _authSettings;
        private readonly StudentApplicationSettings _appSettings;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<StudentAuthService> _logger;

        /// Real hash used to equalise timing when the identifier does not resolve, so an unknown
        /// account costs roughly the same as a wrong password. Computed once per process.
        private static string? _timingGuardHash;

        public StudentAuthService(
            IStudentRepository studentRepository,
            IStudentLoginAttemptRepository loginAttemptRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IOptions<StudentAuthenticationSettings> authSettings,
            IOptions<StudentApplicationSettings> appSettings,
            IOptions<JwtSettings> jwtSettings,
            ILogger<StudentAuthService> logger)
        {
            _studentRepository = studentRepository;
            _loginAttemptRepository = loginAttemptRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _authSettings = authSettings.Value;
            _appSettings = appSettings.Value;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<StudentLoginResult> LoginAsync(StudentLoginRequest request)
        {
            // One captured UTC instant drives every timestamp and comparison in this request.
            var nowUtc = DateTime.UtcNow;

            var deviceId = NormalizeDeviceId(request.DeviceId);
            var identifier = request.Identifier?.Trim() ?? string.Empty;

            // Client-version gate runs before any credential work, so an outdated build can never
            // consume an attempt or trigger a lockout.
            if (!IsSupportedClientVersion(request.AppVersion, out var minimumVersion))
            {
                _logger.LogInformation(
                    "Student login rejected: unsupported client version for identifier {Identifier}.",
                    MaskIdentifier(identifier));

                return StudentLoginResult.AppVersionUnsupported(minimumVersion);
            }

            var student = await ResolveStudentAsync(identifier);

            if (student == null)
                return await HandleUnresolvedIdentifierAsync(identifier, request.Password ?? string.Empty, nowUtc);

            // An expired lockout is cleared first, then the attempt is processed from a clean counter.
            if (student.lockout_end_utc.HasValue)
            {
                if (student.lockout_end_utc.Value > nowUtc)
                {
                    var retryAfterSeconds = ToRetryAfterSeconds(student.lockout_end_utc.Value, nowUtc);

                    _logger.LogWarning(
                        "Student login blocked: student {StudentId} is locked out for another {RetryAfterSeconds}s.",
                        student.id, retryAfterSeconds);

                    return StudentLoginResult.AccountLocked(retryAfterSeconds);
                }

                await _studentRepository.ResetLoginStateAsync(student.id, nowUtc);
                student.failed_login_attempts = 0;
                student.lockout_end_utc = null;
            }

            if (!VerifyPassword(request.Password ?? string.Empty, student.password))
                return await RegisterFailedAttemptAsync(student, nowUtc);

            // Password is correct from here on: inactivity is only revealed to the real account holder.
            if (!student.is_active)
            {
                _logger.LogWarning(
                    "Student login rejected: student {StudentId} is inactive.", student.id);

                return StudentLoginResult.AccountInactive();
            }

            if (student.failed_login_attempts != 0 || student.lockout_end_utc.HasValue)
                await _studentRepository.ResetLoginStateAsync(student.id, nowUtc);

            var (accessToken, expiresAtUtc) = _jwtTokenGenerator.GenerateStudentAccessToken(student, deviceId);

            _logger.LogInformation("Student login succeeded for student {StudentId}.", student.id);

            return StudentLoginResult.Success(new StudentLoginResponse
            {
                AccessToken = accessToken,
                ExpiresAtUtc = expiresAtUtc,
                ServerTimeUtc = nowUtc,
                StudentData = new StudentDataResponse
                {
                    Id = student.id,
                    UserName = student.user_name,
                    Email = student.email,
                    UniversityNumber = student.university_number,
                    FullName = BuildFullName(student),
                },
            });
        }

        /// Mirrors the real-account failure path for an identifier that does not resolve: same lockout check,
        /// same BCrypt cost, same attempt countdown and same lockout response. Without this, the decreasing
        /// remainingAttempts (and the eventual ACCOUNT_LOCKED) of a real account would disclose which
        /// identifiers exist.
        private async Task<StudentLoginResult> HandleUnresolvedIdentifierAsync(
            string identifier, string password, DateTime nowUtc)
        {
            var identifierHash = HashIdentifier(identifier);
            var tracked = await _loginAttemptRepository.GetByIdentifierHashAsync(identifierHash);

            if (tracked?.lockout_end_utc != null)
            {
                // Locked: skip verification exactly like a locked real account, so timing matches too.
                if (tracked.lockout_end_utc.Value > nowUtc)
                {
                    _logger.LogWarning(
                        "Student login blocked: unresolved identifier {Identifier} is locked out.",
                        MaskIdentifier(identifier));

                    return StudentLoginResult.AccountLocked(
                        ToRetryAfterSeconds(tracked.lockout_end_utc.Value, nowUtc));
                }

                await _loginAttemptRepository.ResetAsync(identifierHash, nowUtc);
            }

            // Spend comparable time so an unknown account is not distinguishable by latency.
            VerifyPassword(password, TimingGuardHash());

            var lockoutEndUtc = nowUtc.AddMinutes(_authSettings.LockoutDurationMinutes);

            var (failedAttempts, persistedLockoutEndUtc) = await _loginAttemptRepository
                .RegisterFailedAttemptAsync(identifierHash, _authSettings.MaxFailedLoginAttempts, lockoutEndUtc, nowUtc);

            if (persistedLockoutEndUtc.HasValue && persistedLockoutEndUtc.Value > nowUtc)
            {
                _logger.LogWarning(
                    "Unresolved identifier {Identifier} locked out after {FailedAttempts} failed attempts.",
                    MaskIdentifier(identifier), failedAttempts);

                return StudentLoginResult.AccountLocked(
                    ToRetryAfterSeconds(persistedLockoutEndUtc.Value, nowUtc));
            }

            _logger.LogInformation(
                "Student login failed: identifier {Identifier} did not resolve; {RemainingAttempts} attempt(s) remaining.",
                MaskIdentifier(identifier), Math.Max(0, _authSettings.MaxFailedLoginAttempts - failedAttempts));

            return StudentLoginResult.InvalidCredentials(
                Math.Max(0, _authSettings.MaxFailedLoginAttempts - failedAttempts));
        }

        /// Keyed hash (HMAC-SHA256 under the JWT signing key) of the normalized identifier, so the tracking
        /// table never holds a raw email or username and cannot be dictionary-attacked without the key.
        private string HashIdentifier(string identifier)
        {
            var normalized = identifier.Trim().ToLowerInvariant();

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized));

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private async Task<Student?> ResolveStudentAsync(string identifier)
        {
            if (identifier.Length == 0)
                return null;

            // An identifier containing '@' is an email and is never retried as a username, and vice versa.
            return identifier.Contains('@')
                ? await _studentRepository.GetByEmailAsync(identifier)
                : await _studentRepository.GetByUserNameAsync(identifier);
        }

        private async Task<StudentLoginResult> RegisterFailedAttemptAsync(Student student, DateTime nowUtc)
        {
            var lockoutEndUtc = nowUtc.AddMinutes(_authSettings.LockoutDurationMinutes);

            var (failedAttempts, persistedLockoutEndUtc) = await _studentRepository.RegisterFailedLoginAsync(
                student.id, _authSettings.MaxFailedLoginAttempts, lockoutEndUtc, nowUtc);

            if (persistedLockoutEndUtc.HasValue && persistedLockoutEndUtc.Value > nowUtc)
            {
                var retryAfterSeconds = ToRetryAfterSeconds(persistedLockoutEndUtc.Value, nowUtc);

                _logger.LogWarning(
                    "Student {StudentId} locked out after {FailedAttempts} failed attempts for {RetryAfterSeconds}s.",
                    student.id, failedAttempts, retryAfterSeconds);

                return StudentLoginResult.AccountLocked(retryAfterSeconds);
            }

            var remaining = Math.Max(0, _authSettings.MaxFailedLoginAttempts - failedAttempts);

            _logger.LogInformation(
                "Student login failed for student {StudentId}; {RemainingAttempts} attempt(s) remaining.",
                student.id, remaining);

            return StudentLoginResult.InvalidCredentials(remaining);
        }

        private bool IsSupportedClientVersion(string? submittedVersion, out string minimumVersion)
        {
            // Startup validation guarantees the configured minimum parses.
            AppVersion.TryParse(_appSettings.MinimumSupportedVersion, out var minimum);
            minimumVersion = _appSettings.MinimumSupportedVersion;

            // Malformed versions are rejected by request validation before reaching this point.
            return AppVersion.TryParse(submittedVersion, out var submitted) && submitted >= minimum;
        }

        /// Canonical "D" form (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx) of an already validated UUID.
        private static string NormalizeDeviceId(string? deviceId) =>
            Guid.TryParse(deviceId, out var parsed) ? parsed.ToString("D") : string.Empty;

        /// Never negative, even if the lockout expires between the read and this call.
        private static int ToRetryAfterSeconds(DateTime lockoutEndUtc, DateTime nowUtc) =>
            Math.Max(0, (int)Math.Ceiling((lockoutEndUtc - nowUtc).TotalSeconds));

        /// BCrypt-only verification. A stored value that is not a BCrypt hash (for example a row imported
        /// with a plaintext password) makes the hasher throw; that is treated as a failed login rather than
        /// as a plaintext fallback, and no stored value is ever rewritten during authentication.
        private bool VerifyPassword(string password, string storedPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(storedPasswordHash))
                return false;

            try
            {
                return _passwordHasher.Verify(password, storedPasswordHash);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string TimingGuardHash() =>
            _timingGuardHash ??= _passwordHasher.Hash(Guid.NewGuid().ToString("N"));

        /// Keeps identifiers out of logs in raw form: first character plus length only.
        private static string MaskIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return "(empty)";

            return $"{identifier[0]}***({identifier.Length})";
        }

        private static string BuildFullName(Student student)
        {
            var parts = new List<string?> { student.first_name, student.middle_name, student.last_name };

            return string.Join(" ", parts
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));
        }
    }
}
