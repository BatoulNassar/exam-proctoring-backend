using ExamProctoring.Application.Features.Streaming.DTOs;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// Real-time fan-out for Stream-layer presence and WebRTC signaling.
    /// Application stays free of SignalR types; the API adapter implements this.
    public interface IStreamSignalingNotifier
    {
        Task NotifyWatchRequestedAsync(
            int studentSessionId,
            string? proctorConnectionId,
            StreamWatchRequestedDto payload);

        Task NotifyOfferAsync(string proctorConnectionId, StreamSdpDto payload);

        Task NotifyAnswerAsync(string studentConnectionId, StreamSdpDto payload);

        Task NotifyIceCandidateAsync(string targetConnectionId, StreamIceCandidateDto payload);

        Task NotifyWatchEndedAsync(
            string? proctorConnectionId,
            string? studentConnectionId,
            StreamWatchEndedDto payload);

        Task NotifyWatchRejectedAsync(string proctorConnectionId, StreamWatchRejectedDto payload);

        Task NotifyStudentConnectedAsync(int examSessionId, StudentHubConnectedDto payload);

        Task NotifyStudentDisconnectedAsync(int examSessionId, StudentHubDisconnectedDto payload);

        Task NotifyPresenceSnapshotAsync(string connectionId, StudentHubPresenceSnapshotDto payload);
    }
}
