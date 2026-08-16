using ExamProctoring.API.Hubs;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Streaming.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace ExamProctoring.API.Services
{
    /// SignalR adapter for Stream-layer events. Event names match PROCTORING_REALTIME_CONTRACT.
    public sealed class SignalRStreamSignalingNotifier : IStreamSignalingNotifier
    {
        private readonly IHubContext<MonitoringHub> _hub;

        public SignalRStreamSignalingNotifier(IHubContext<MonitoringHub> hub)
        {
            _hub = hub;
        }

        private IClientProxy Session(int examSessionId) =>
            _hub.Clients.Group($"{MonitoringHub.SessionGroupPrefix}{examSessionId}");

        private IClientProxy StudentSession(int studentSessionId) =>
            _hub.Clients.Group($"{MonitoringHub.StudentSessionGroupPrefix}{studentSessionId}");

        public async Task NotifyWatchRequestedAsync(
            int studentSessionId,
            string? proctorConnectionId,
            StreamWatchRequestedDto payload)
        {
            await StudentSession(studentSessionId).SendAsync("StreamWatchRequested", payload);
            if (!string.IsNullOrEmpty(proctorConnectionId))
                await _hub.Clients.Client(proctorConnectionId).SendAsync("StreamWatchRequested", payload);
        }

        public Task NotifyOfferAsync(string proctorConnectionId, StreamSdpDto payload) =>
            _hub.Clients.Client(proctorConnectionId).SendAsync("StreamOffer", payload);

        public Task NotifyAnswerAsync(string studentConnectionId, StreamSdpDto payload) =>
            _hub.Clients.Client(studentConnectionId).SendAsync("StreamAnswer", payload);

        public Task NotifyIceCandidateAsync(string targetConnectionId, StreamIceCandidateDto payload) =>
            _hub.Clients.Client(targetConnectionId).SendAsync("IceCandidate", payload);

        public async Task NotifyWatchEndedAsync(
            string? proctorConnectionId,
            string? studentConnectionId,
            StreamWatchEndedDto payload)
        {
            if (!string.IsNullOrEmpty(proctorConnectionId))
                await _hub.Clients.Client(proctorConnectionId).SendAsync("StreamWatchEnded", payload);
            if (!string.IsNullOrEmpty(studentConnectionId)
                && !string.Equals(studentConnectionId, proctorConnectionId, StringComparison.Ordinal))
            {
                await _hub.Clients.Client(studentConnectionId).SendAsync("StreamWatchEnded", payload);
            }
        }

        public Task NotifyWatchRejectedAsync(string proctorConnectionId, StreamWatchRejectedDto payload) =>
            _hub.Clients.Client(proctorConnectionId).SendAsync("StreamWatchRejected", payload);

        public Task NotifyStudentConnectedAsync(int examSessionId, StudentHubConnectedDto payload) =>
            Session(examSessionId).SendAsync("StudentHubConnected", payload);

        public Task NotifyStudentDisconnectedAsync(int examSessionId, StudentHubDisconnectedDto payload) =>
            Session(examSessionId).SendAsync("StudentHubDisconnected", payload);

        public Task NotifyPresenceSnapshotAsync(string connectionId, StudentHubPresenceSnapshotDto payload) =>
            _hub.Clients.Client(connectionId).SendAsync("StudentHubPresenceSnapshot", payload);
    }
}
