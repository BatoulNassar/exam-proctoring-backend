using ExamProctoring.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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
    public class MonitoringHub : Hub
    {
        public const string SessionGroupPrefix = "session-";
        public const string StudentSessionGroupPrefix = "student-session-";

        private readonly IProctorSessionRepository _proctorSessionRepository;
        private readonly IStudentSessionRepository _studentSessionRepository;

        public MonitoringHub(
            IProctorSessionRepository proctorSessionRepository,
            IStudentSessionRepository studentSessionRepository)
        {
            _proctorSessionRepository = proctorSessionRepository;
            _studentSessionRepository = studentSessionRepository;
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

        /// <summary>
        /// Subscribes a proctor or admin to an exam session's live feed. A proctor
        /// must be assigned to it; admins and super admins may watch any session.
        /// </summary>
        public async Task JoinSession(int sessionId)
        {
            if (IsStudentToken())
                throw new HubException("Student clients cannot watch a session feed");

            var isPrivileged = Context.User?.IsInRole("SuperAdmin") == true
                            || Context.User?.IsInRole("Admin") == true;

            if (!isPrivileged)
            {
                var proctorId = GetUserId();
                if (proctorId == null)
                    throw new HubException("Invalid user identity");

                var assigned = await _proctorSessionRepository.GetSessionIdsByProctorAsync(proctorId.Value);
                if (!assigned.Contains(sessionId))
                    throw new HubException("You are not assigned to this exam session");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"{SessionGroupPrefix}{sessionId}");
        }

        public async Task LeaveSession(int sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{SessionGroupPrefix}{sessionId}");
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

            await Groups.AddToGroupAsync(Context.ConnectionId, $"{StudentSessionGroupPrefix}{studentSessionId}");
        }

        public async Task LeaveStudentSession(int studentSessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{StudentSessionGroupPrefix}{studentSessionId}");
        }
    }
}
