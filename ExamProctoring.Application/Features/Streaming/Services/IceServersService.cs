using ExamProctoring.Application.Common.Settings;
using ExamProctoring.Application.Features.Streaming.DTOs;
using Microsoft.Extensions.Options;

namespace ExamProctoring.Application.Features.Streaming.Services
{
    public sealed class IceServersService : IIceServersService
    {
        private readonly WebRtcSettings _settings;

        public IceServersService(IOptions<WebRtcSettings> settings)
        {
            _settings = settings.Value;
        }

        public IceServersResponseDto? GetIceServers()
        {
            var stun = (_settings.StunUrls ?? Array.Empty<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (stun.Length == 0)
                return null;

            var servers = new List<IceServerEntryDto>
            {
                new() { Urls = stun },
            };

            var turnUrls = (_settings.TurnUrls ?? Array.Empty<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (turnUrls.Length > 0)
            {
                servers.Add(new IceServerEntryDto
                {
                    Urls = turnUrls,
                    Username = string.IsNullOrWhiteSpace(_settings.TurnUsername)
                        ? null
                        : _settings.TurnUsername,
                    Credential = string.IsNullOrWhiteSpace(_settings.TurnCredential)
                        ? null
                        : _settings.TurnCredential,
                });
            }

            DateTime? expires = null;
            if (_settings.IceCredentialTtlMinutes is int ttl && ttl > 0)
                expires = DateTime.UtcNow.AddMinutes(ttl);

            return new IceServersResponseDto
            {
                IceServers = servers,
                ExpiresAtUtc = expires,
            };
        }
    }
}
