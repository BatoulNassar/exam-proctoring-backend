using ExamProctoring.Application.Features.Monitoring.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace ExamProctoring.API.Hubs
{
    public partial class MonitoringHub
    {
        /// <summary>
        /// Pipeline-alive signal from the student MonitoringEngine (realtime contract §10).
        /// </summary>
        public async Task Heartbeat(HeartbeatDto payload)
        {
            if (!IsStudentToken())
                throw new HubException("Only the student desktop client can send heartbeats");

            if (payload == null)
                throw new HubException("Heartbeat payload is required");

            if (!PipelineStatuses.IsKnown(payload.PipelineStatus))
                throw new HubException("Invalid pipelineStatus");

            var studentId = GetStudentId();
            if (studentId == null)
                throw new HubException("Invalid student identity");

            var studentSession = await _studentSessionRepository.GetByIdAsync(payload.StudentSessionId);
            if (studentSession == null || studentSession.student_id != studentId.Value)
                throw new HubException("This attempt does not belong to you");

            var serverUtc = DateTime.UtcNow;
            if (!_presence.TryRecordHeartbeat(
                    payload.StudentSessionId,
                    Context.ConnectionId,
                    payload.PipelineStatus,
                    serverUtc,
                    out _,
                    out var entry)
                || entry == null)
            {
                throw new HubException("Join the student session hub group before sending heartbeats");
            }

            // Always fan-out freshness. Dashboard §10.3 staleness compares wall-clock to
            // lastHeartbeatAtUtc; pipeline-change-only pushes freeze that timestamp and
            // falsely show "Monitoring degraded" while detectors/REST alerts still work.
            await _monitoringNotifier.NotifyStudentStatusChangedAsync(
                entry.ExamSessionId,
                new StudentStatusChangedDto
                {
                    StudentSessionId = entry.StudentSessionId,
                    NewStatus = studentSession.status.ToString(),
                    LoginAtUtc = studentSession.login_at,
                    PipelineStatus = entry.PipelineStatus,
                    LastHeartbeatAtUtc = entry.LastHeartbeatAtUtc,
                });
        }
    }
}
