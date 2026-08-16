using ExamProctoring.API.Hubs;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Alerts.DTOs;
using ExamProctoring.Application.Features.Monitoring.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace ExamProctoring.API.Services
{
    public class SignalRMonitoringNotifier : IMonitoringNotifier
    {
        private readonly IHubContext<MonitoringHub> _hub;

        public SignalRMonitoringNotifier(IHubContext<MonitoringHub> hub)
        {
            _hub = hub;
        }

        private IClientProxy Session(int sessionId) =>
            _hub.Clients.Group($"{MonitoringHub.SessionGroupPrefix}{sessionId}");

        private IClientProxy StudentSession(int studentSessionId) =>
            _hub.Clients.Group($"{MonitoringHub.StudentSessionGroupPrefix}{studentSessionId}");

        public Task NotifyAlertUpdatedAsync(int sessionId, int alertId, string newStatus, string? actionTaken) =>
            Session(sessionId).SendAsync("AlertUpdated", new { alertId, newStatus, actionTaken });

        public Task NotifyAlertCreatedAsync(int sessionId, AlertEventDto alert) =>
            Session(sessionId).SendAsync("AlertCreated", alert);

        public Task NotifyStudentStatusChangedAsync(int examSessionId, StudentStatusChangedDto status) =>
            Session(examSessionId).SendAsync("StudentStatusChanged", new
            {
                studentSessionId = status.StudentSessionId,
                newStatus = status.NewStatus,
                loginAtUtc = status.LoginAtUtc,
                pipelineStatus = status.PipelineStatus,
                lastHeartbeatAtUtc = status.LastHeartbeatAtUtc,
            });

        public Task NotifyStudentWarnedAsync(int studentSessionId, string message, int warningNumber, int maxWarnings) =>
            StudentSession(studentSessionId).SendAsync("WarningReceived", new { message, warningNumber, maxWarnings });

        public Task NotifyStudentSessionTerminatedAsync(int studentSessionId, string reason) =>
            StudentSession(studentSessionId).SendAsync("SessionTerminated", new { reason });
    }
}
