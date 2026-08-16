using ExamProctoring.Application.Features.Streaming.DTOs;

namespace ExamProctoring.Application.Features.Streaming.Services
{
    public interface IIceServersService
    {
        /// <summary>
        /// Builds the ICE list from configuration. Returns null when no STUN URL is available
        /// (caller maps to ICE_CONFIG_UNAVAILABLE).
        /// </summary>
        IceServersResponseDto? GetIceServers();
    }
}
