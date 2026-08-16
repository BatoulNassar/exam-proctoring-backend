using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Common.Settings;
using ExamProctoring.Application.Features.Streaming.DTOs;
using ExamProctoring.Application.Features.Streaming.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExamProctoring.API.Hubs
{
    /// <summary>
    /// Two kinds of group live here:
    /// <c>session-{examSessionId}</c> for the proctors watching an exam, and
    /// <c>student-session-{studentSessionId}</c> for the one student sitting it.
    /// Membership is verified against the caller's token — a group name in a
    /// request is a claim, not a permission.
    /// </summary>
    [Authorize]
    public partial class MonitoringHub : Hub
    {
        public const string SessionGroupPrefix = "session-";
        public const string StudentSessionGroupPrefix = "student-session-";
        public const int MaxSignalingPayloadBytes = 64 * 1024;

        private readonly IProctorSessionRepository _proctorSessionRepository;
        private readonly IStudentSessionRepository _studentSessionRepository;
        private readonly IStudentHubPresence _presence;
        private readonly IStreamWatchRegistry _watches;
        private readonly IStreamSignalingNotifier _streamNotifier;
        private readonly IMonitoringNotifier _monitoringNotifier;
        private readonly WebRtcSettings _webRtc;
        private readonly ILogger<MonitoringHub> _logger;

        public MonitoringHub(
            IProctorSessionRepository proctorSessionRepository,
            IStudentSessionRepository studentSessionRepository,
            IStudentHubPresence presence,
            IStreamWatchRegistry watches,
            IStreamSignalingNotifier streamNotifier,
            IMonitoringNotifier monitoringNotifier,
            IOptions<WebRtcSettings> webRtc,
            ILogger<MonitoringHub> logger)
        {
            _proctorSessionRepository = proctorSessionRepository;
            _studentSessionRepository = studentSessionRepository;
            _presence = presence;
            _watches = watches;
            _streamNotifier = streamNotifier;
            _monitoringNotifier = monitoringNotifier;
            _webRtc = webRtc.Value;
            _logger = logger;
        }

        private bool IsStudentToken() =>
            Context.User?.HasClaim(c => c.Type == "token_type" && c.Value == "student") == true;

        private int? GetStudentId() =>
            int.TryParse(Context.User?.FindFirst("student_id")?.Value, out var id) ? id : null;

        private int? GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(claim, out var id) ? id : null;
        }

        private bool IsPrivilegedDashboardUser() =>
            Context.User?.IsInRole("SuperAdmin") == true
            || Context.User?.IsInRole("Admin") == true;

        /// <summary>
        /// Subscribes a proctor or admin to an exam session's live feed. A proctor
        /// must be assigned to it; admins and super admins may watch any session.
        /// </summary>
        public async Task JoinSession(int sessionId)
        {
            if (IsStudentToken())
                throw new HubException("Student clients cannot watch a session feed");

            if (!IsPrivilegedDashboardUser())
            {
                var proctorId = GetUserId();
                if (proctorId == null)
                    throw new HubException("Invalid user identity");

                var assigned = await _proctorSessionRepository.GetSessionIdsByProctorAsync(proctorId.Value);
                if (!assigned.Contains(sessionId))
                    throw new HubException("You are not assigned to this exam session");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"{SessionGroupPrefix}{sessionId}");
            _presence.AddJoinedExamSession(Context.ConnectionId, sessionId);

            await _streamNotifier.NotifyPresenceSnapshotAsync(
                Context.ConnectionId,
                new StudentHubPresenceSnapshotDto
                {
                    ExamSessionId = sessionId,
                    ConnectedStudentSessionIds = _presence.GetConnectedStudentSessionIds(sessionId),
                });
        }

        public async Task LeaveSession(int sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{SessionGroupPrefix}{sessionId}");
            _presence.RemoveJoinedExamSession(Context.ConnectionId, sessionId);
        }

        /// <summary>
        /// Subscribes the student desktop client to its own attempt, so warnings and
        /// termination reach the screen. A student can only join their own.
        /// </summary>
        public async Task JoinStudentSession(int studentSessionId)
        {
            if (!IsStudentToken())
                throw new HubException("Only the student desktop client can join a student session");

            var studentId = GetStudentId();
            if (studentId == null)
                throw new HubException("Invalid student identity");

            var studentSession = await _studentSessionRepository.GetByIdAsync(studentSessionId);
            if (studentSession == null || studentSession.student_id != studentId.Value)
                throw new HubException("This attempt does not belong to you");

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"{StudentSessionGroupPrefix}{studentSessionId}");

            var examSessionId = studentSession.exam_session_id;
            _presence.SetStudentConnected(examSessionId, studentSessionId, Context.ConnectionId);

            await _streamNotifier.NotifyStudentConnectedAsync(
                examSessionId,
                new StudentHubConnectedDto
                {
                    StudentSessionId = studentSessionId,
                    ConnectedAtUtc = DateTime.UtcNow,
                });
        }

        public async Task LeaveStudentSession(int studentSessionId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"{StudentSessionGroupPrefix}{studentSessionId}");

            if (_watches.TryRemoveForStudentSession(studentSessionId, out var watch))
            {
                await _streamNotifier.NotifyWatchEndedAsync(
                    watch.ProctorConnectionId,
                    watch.StudentConnectionId,
                    new StreamWatchEndedDto
                    {
                        WatchId = watch.WatchId,
                        StudentSessionId = watch.StudentSessionId,
                        Reason = StreamWatchCodes.StudentEnded,
                        EndedAtUtc = DateTime.UtcNow,
                    });

                _logger.LogInformation(
                    "Stream watch {WatchId} ended ({Reason}) for studentSession {StudentSessionId}",
                    watch.WatchId,
                    StreamWatchCodes.StudentEnded,
                    studentSessionId);
            }

            var cleared = _presence.ClearStudentBySession(studentSessionId, Context.ConnectionId);
            if (cleared != null)
            {
                await _streamNotifier.NotifyStudentDisconnectedAsync(
                    cleared.ExamSessionId,
                    new StudentHubDisconnectedDto
                    {
                        StudentSessionId = cleared.StudentSessionId,
                        DisconnectedAtUtc = DateTime.UtcNow,
                        Reason = StreamWatchCodes.DisconnectReasonLeave,
                    });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            foreach (var watch in _watches.RemoveAllForConnection(connectionId))
            {
                var reason = string.Equals(
                    watch.ProctorConnectionId,
                    connectionId,
                    StringComparison.Ordinal)
                    ? StreamWatchCodes.ProctorDisconnected
                    : StreamWatchCodes.StudentDisconnected;

                await _streamNotifier.NotifyWatchEndedAsync(
                    watch.ProctorConnectionId,
                    watch.StudentConnectionId,
                    new StreamWatchEndedDto
                    {
                        WatchId = watch.WatchId,
                        StudentSessionId = watch.StudentSessionId,
                        Reason = reason,
                        EndedAtUtc = DateTime.UtcNow,
                    });

                _logger.LogInformation(
                    "Stream watch {WatchId} ended ({Reason}) after disconnect",
                    watch.WatchId,
                    reason);
            }

            var studentPresence = _presence.ClearStudentByConnection(connectionId);
            if (studentPresence != null)
            {
                await _streamNotifier.NotifyStudentDisconnectedAsync(
                    studentPresence.ExamSessionId,
                    new StudentHubDisconnectedDto
                    {
                        StudentSessionId = studentPresence.StudentSessionId,
                        DisconnectedAtUtc = DateTime.UtcNow,
                        Reason = StreamWatchCodes.DisconnectReasonConnectionLost,
                    });
            }

            _presence.ClearDashboardConnection(connectionId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
