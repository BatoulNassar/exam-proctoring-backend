using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Alerts;
using ExamProctoring.Application.Features.Alerts.DTOs;
using ExamProctoring.Application.Features.AuditLogs.Services;
using ExamProctoring.Application.Features.ExamAttempts.Services;
using ExamProctoring.Application.Features.Monitoring.DTOs;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Alerts.Services
{
    public class AlertService : IAlertService
    {
        private readonly IAlertEventRepository _alertRepository;
        private readonly IProctorActionRepository _actionRepository;
        private readonly IProctorSessionRepository _proctorSessionRepository;
        private readonly IStudentSessionRepository _studentSessionRepository;
        private readonly ISystemSettingsRepository _settingsRepository;
        private readonly IAuditLogService _auditLog;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMonitoringNotifier _notifier;
        private readonly IAttemptFinalisationService _finalisationService;

        public AlertService(
            IAlertEventRepository alertRepository,
            IProctorActionRepository actionRepository,
            IProctorSessionRepository proctorSessionRepository,
            IStudentSessionRepository studentSessionRepository,
            ISystemSettingsRepository settingsRepository,
            IAuditLogService auditLog,
            IUnitOfWork unitOfWork,
            IMonitoringNotifier notifier,
            IAttemptFinalisationService finalisationService)
        {
            _finalisationService = finalisationService;
            _alertRepository = alertRepository;
            _actionRepository = actionRepository;
            _proctorSessionRepository = proctorSessionRepository;
            _studentSessionRepository = studentSessionRepository;
            _settingsRepository = settingsRepository;
            _auditLog = auditLog;
            _unitOfWork = unitOfWork;
            _notifier = notifier;
        }

        /// <summary>
        /// Ends the student's own attempt. Never touches the ExamSession, which stays
        /// running for everyone else in the room.
        /// </summary>
        private async Task<bool> TerminateStudentSessionAsync(int studentSessionId, int actorId, string reason)
        {
            var studentSession = await _studentSessionRepository.GetByIdAsync(studentSessionId);

            if (studentSession == null || studentSession.finalised_at.HasValue)
                return false;

            // Routed through the one shared finalisation path rather than setting the status here,
            // so a terminated attempt receives the same frozen result as a submitted one: an
            // answered count, a receipt and a finalised_at. Before this, a terminated attempt had
            // a status but no receipt, and a later student Submit had nothing frozen to return.
            var outcome = await _finalisationService.FinaliseAsync(new AttemptFinalisationContext
            {
                StudentSessionId = studentSessionId,
                ExamSessionId = studentSession.exam_session_id,
                CourseTag = studentSession.ExamSession?.course_tag ?? string.Empty,
                QuestionCount = studentSession.question_count ?? 0,
                Reason = AttemptFinalisationReason.ProctorTerminated,
                IsAlreadyTerminated = studentSession.status == StudentSessionStatus.Terminated,
                ActorId = actorId,
                ActorType = "Admin",
            });

            if (outcome.Status != AttemptFinalisationStatus.Finalised)
                return false;

            // The shared path writes its own audit row inside the finalisation transaction; this
            // one records the proctor-facing reason, which that generic row does not carry.
            await _auditLog.LogAsync(
                studentSession.exam_session_id, actorId, "Admin", "StudentSessionTerminated",
                studentSessionId, "StudentSession", reason);

            return true;
        }

        /// <summary>
        /// Fan-out for a termination. Kept separate from the database work above so it
        /// runs only after the transaction commits — pushing "your exam is over" and
        /// then failing to save would be worse than a delayed notification.
        /// </summary>
        private async Task NotifyTerminationAsync(int examSessionId, int studentSessionId, string reason)
        {
            await _notifier.NotifyStudentSessionTerminatedAsync(studentSessionId, reason);
            await _notifier.NotifyStudentStatusChangedAsync(
                examSessionId,
                new StudentStatusChangedDto
                {
                    StudentSessionId = studentSessionId,
                    NewStatus = StudentSessionStatus.Terminated.ToString(),
                });
        }

        public IReadOnlyList<AlertTypeDto> GetAlertTypes() => AlertTypeCatalog.All;

        /// <summary>
        /// The exam sessions a proctor is allowed to see, or null when the caller
        /// has no restriction (admins and super admins).
        /// </summary>
        private async Task<IReadOnlyCollection<int>> ResolveScopeAsync(int? restrictToProctorId)
        {
            if (restrictToProctorId == null) return null;

            return await _proctorSessionRepository.GetSessionIdsByProctorAsync(restrictToProctorId.Value);
        }

        public async Task<AlertSummaryDto> GetAlertsSummaryAsync(int? restrictToProctorId = null)
        {
            var scope = await ResolveScopeAsync(restrictToProctorId);
            var all = await _alertRepository.GetAlertsByFilterAsync(null, null, null, scope);

            return new AlertSummaryDto
            {
                TotalAlerts = all.Count(),
                CriticalAlerts = all.Count(a => a.severity == AlertSeverity.Critical),
                WarningAlerts = all.Count(a => a.severity == AlertSeverity.Warning),
                ResolvedAlerts = all.Count(a => a.status == AlertStatus.Resolved),
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<(IEnumerable<AlertEventDto> Items, int TotalCount)> GetAlertsAsync(
            AlertStatus? status,
            string alertType,
            int? sessionId,
            int page,
            int pageSize,
            int? restrictToProctorId = null)
        {
            var scope = await ResolveScopeAsync(restrictToProctorId);
            var alerts = await _alertRepository.GetAlertsByFilterAsync(status, alertType, sessionId, scope);

            var totalCount = alerts.Count();
            var items = alerts
                .OrderByDescending(a => a.triggered_at)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => MapToDto(a))
                .ToList();

            return (items, totalCount);
        }

        public async Task<(bool Success, string Message)> DismissAlertAsync(int alertId, int adminId)
        {
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null)
                return (false, "Alert not found");

            if (alert.status != AlertStatus.Open)
                return (false, "Alert is not in Open status");

            alert.status = AlertStatus.Resolved;
            await _alertRepository.UpdateAsync(alert);

            await _actionRepository.AddAsync(new ProctorAction
            {
                alert_event_id = alertId,
                admin_id = adminId,
                action_type = ProctorActionType.Dismiss,
                action_note = "Alert dismissed",
                acted_at = DateTime.UtcNow
            });

            var sessionId = alert.StudentSession?.exam_session_id ?? 0;

            await _auditLog.LogAsync(
                sessionId, adminId, "Admin", "AlertDismissed", alertId, "AlertEvent",
                $"Dismissed {alert.alert_type} alert for student session {alert.student_session_id}");

            await _unitOfWork.SaveChangesAsync();

            await _notifier.NotifyAlertUpdatedAsync(sessionId, alertId, "Resolved", "Dismiss");

            return (true, "Alert dismissed successfully");
        }

        public async Task<(bool Success, string Message)> WarnStudentAsync(int alertId, int adminId, string message)
        {
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null)
                return (false, "Alert not found");

            if (string.IsNullOrWhiteSpace(message))
                return (false, "Warning message is required");

            var studentStatus = alert.StudentSession?.status;
            if (studentStatus == StudentSessionStatus.Terminated)
                return (false, "The student's session has already been terminated");
            if (studentStatus == StudentSessionStatus.Submitted)
                return (false, "The student has already submitted, a warning would not reach them");

            var now = DateTime.UtcNow;
            var sessionId = alert.StudentSession?.exam_session_id ?? 0;

            // Counted before the new action is added so the total is unambiguous.
            var warningCount = await _actionRepository.CountWarningsForStudentSessionAsync(alert.student_session_id) + 1;

            var action = new ProctorAction
            {
                alert_event_id = alertId,
                admin_id = adminId,
                action_type = ProctorActionType.Warn,
                action_note = message,
                acted_at = now,
                created_at = now,
                created_by = adminId
            };

            await _actionRepository.AddAsync(action);

            // The durable record of what the student was told. The action row alone
            // says a warning happened; this is the message itself.
            await _studentSessionRepository.AddWarningMessageAsync(new WarningMessage
            {
                ProctorAction = action,
                student_session_id = alert.student_session_id,
                message_text = message,
                sent_at = now,
                created_at = now,
                created_by = adminId
            });

            await _auditLog.LogAsync(
                sessionId, adminId, "Admin", "StudentWarned", alertId, "AlertEvent",
                $"Warned student session {alert.student_session_id}: {message}");

            var settings = await _settingsRepository.GetAsync();
            var threshold = settings?.max_warnings_before_termination ?? 0;

            var terminationReason = $"Reached {warningCount} warnings (limit {threshold})";

            var terminated = false;
            if (threshold > 0 && warningCount >= threshold)
            {
                terminated = await TerminateStudentSessionAsync(
                    alert.student_session_id, adminId, terminationReason);
            }

            await _unitOfWork.SaveChangesAsync();

            // Only after the commit: this is what actually puts the warning on the
            // student's screen, so it must not fire for a warning that was not saved.
            await _notifier.NotifyStudentWarnedAsync(alert.student_session_id, message, warningCount, threshold);
            await _notifier.NotifyAlertUpdatedAsync(sessionId, alertId, alert.status.ToString(), "Warn");

            if (terminated)
                await NotifyTerminationAsync(sessionId, alert.student_session_id, terminationReason);

            return terminated
                ? (true, $"Warning sent. This was warning {warningCount} of {threshold}, so the student's session was terminated")
                : (true, $"Warning sent to student successfully ({warningCount} of {threshold})");
        }

        public async Task<(bool Success, string Message)> EscalateAlertAsync(int alertId, int adminId, string reason)
        {
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null)
                return (false, "Alert not found");

            // Guard the state so a double click cannot escalate twice, and so an
            // already-resolved alert cannot be revived into a termination.
            if (alert.status != AlertStatus.Open)
                return (false, $"Alert is already {alert.status} and cannot be escalated");

            if (string.IsNullOrWhiteSpace(reason))
                return (false, "An escalation reason is required");

            if (alert.StudentSession?.status == StudentSessionStatus.Terminated)
                return (false, "The student's session has already been terminated");

            var now = DateTime.UtcNow;
            var sessionId = alert.StudentSession?.exam_session_id ?? 0;

            alert.status = AlertStatus.Escalated;
            await _alertRepository.UpdateAsync(alert);

            await _actionRepository.AddAsync(new ProctorAction
            {
                alert_event_id = alertId,
                admin_id = adminId,
                action_type = ProctorActionType.Escalate,
                action_note = reason,
                acted_at = now,
                created_at = now,
                created_by = adminId
            });

            await _auditLog.LogAsync(
                sessionId, adminId, "Admin", "AlertEscalated", alertId, "AlertEvent",
                $"Escalated {alert.alert_type} alert for student session {alert.student_session_id}: {reason}");

            var terminationReason = $"Alert {alertId} escalated: {reason}";
            var terminated = await TerminateStudentSessionAsync(alert.student_session_id, adminId, terminationReason);

            await _unitOfWork.SaveChangesAsync();

            await _notifier.NotifyAlertUpdatedAsync(sessionId, alertId, "Escalated", "Escalate");

            if (terminated)
                await NotifyTerminationAsync(sessionId, alert.student_session_id, terminationReason);

            return terminated
                ? (true, "Alert escalated and the student's session was terminated")
                : (true, "Alert escalated for disciplinary review (the student's session was already ended)");
        }

        private AlertEventDto MapToDto(AlertEvent alert)
        {
            var lastAction = alert.ProctorActions?.OrderByDescending(pa => pa.acted_at).FirstOrDefault();

            return new AlertEventDto
            {
                Id = alert.id,
                AlertType = alert.alert_type,
                AlertDescription = AlertTypeCatalog.GetDescription(alert.alert_type ?? ""),
                Severity = alert.severity.ToString(),
                Status = alert.status.ToString(),
                StudentId = alert.StudentSession?.student_id ?? 0,
                StudentName = alert.StudentSession?.Student != null
                    ? $"{alert.StudentSession.Student.first_name} {alert.StudentSession.Student.last_name}".Trim()
                    : "N/A",
                StudentNumber = alert.StudentSession?.Student?.university_number ?? "N/A",
                SessionId = alert.StudentSession?.exam_session_id ?? 0,
                SessionName = alert.StudentSession?.ExamSession?.title ?? "N/A",
                TriggeredAt = alert.triggered_at,
                StudentSessionId = alert.student_session_id,
                SnapshotUrl = alert.snapshot_url,
                ActionTaken = lastAction?.action_type.ToString(),
                ActionNote = lastAction?.action_note,
                ActionAt = lastAction?.acted_at
            };
        }
    }
}
