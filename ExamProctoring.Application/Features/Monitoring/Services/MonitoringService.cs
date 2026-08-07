using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Alerts;
using ExamProctoring.Application.Features.Alerts.DTOs;
using ExamProctoring.Application.Features.Monitoring.DTOs;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;

namespace ExamProctoring.Application.Features.Monitoring.Services
{
    public class MonitoringService : IMonitoringService
    {
        private readonly IExamSessionRepository _sessionRepository;
        private readonly IProctorSessionRepository _proctorSessionRepository;
        private readonly IStudentSessionRepository _studentSessionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMonitoringNotifier _notifier;

        public MonitoringService(
            IExamSessionRepository sessionRepository,
            IProctorSessionRepository proctorSessionRepository,
            IStudentSessionRepository studentSessionRepository,
            IUnitOfWork unitOfWork,
            IMonitoringNotifier notifier)
        {
            _sessionRepository = sessionRepository;
            _proctorSessionRepository = proctorSessionRepository;
            _studentSessionRepository = studentSessionRepository;
            _unitOfWork = unitOfWork;
            _notifier = notifier;
        }

        public async Task<ReportMonitoringEventResult> ReportEventAsync(ReportMonitoringEventRequest request, int studentId)
        {
            // Rejecting unknown types here is what keeps malformed values like
            // "Gaze Deviation" (with a space) out of the database: such a row would
            // never match a filter and would never appear in any chart.
            if (!AlertTypeCatalog.IsKnown(request.EventType))
                return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.UnknownEventType };

            var studentSession = await _studentSessionRepository.GetByIdAsync(request.StudentSessionId);

            if (studentSession == null)
                return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.SessionNotFound };

            if (studentSession.student_id != studentId)
                return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.NotYourSession };

            if (studentSession.status != StudentSessionStatus.InExam)
                return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.SessionNotActive };

            var now = DateTime.UtcNow;

            // The client's clock is advisory: a future or absent timestamp falls back
            // to the server's, so a tampered device cannot backdate or postdate events.
            var occurredAt = request.OccurredAt.HasValue && request.OccurredAt.Value <= now
                ? request.OccurredAt.Value
                : now;

            var monitoringEvent = new MonitoringEvent
            {
                student_session_id = studentSession.id,
                event_type = request.EventType,
                event_details = request.Details,
                occured_at = occurredAt,
                created_at = now
            };

            await _studentSessionRepository.AddMonitoringEventAsync(monitoringEvent);

            var alert = new AlertEvent
            {
                MonitoringEvent = monitoringEvent,
                student_session_id = studentSession.id,
                alert_type = request.EventType,
                // Severity is decided here, not by the client.
                severity = AlertTypeCatalog.GetSeverity(request.EventType),
                status = AlertStatus.Open,
                triggered_at = occurredAt,
                delivered_at = now,
                created_at = now
            };

            await _studentSessionRepository.AddAlertAsync(alert);
            await _unitOfWork.SaveChangesAsync();

            await _notifier.NotifyAlertCreatedAsync(studentSession.exam_session_id, MapToAlertDto(alert, studentSession));

            return new ReportMonitoringEventResult
            {
                Outcome = ReportMonitoringEventOutcome.AlertRaised,
                MonitoringEventId = monitoringEvent.id,
                AlertId = alert.id
            };
        }

        private static AlertEventDto MapToAlertDto(AlertEvent alert, StudentSession studentSession) =>
            new()
            {
                Id = alert.id,
                AlertType = alert.alert_type,
                AlertDescription = AlertTypeCatalog.GetDescription(alert.alert_type),
                Severity = alert.severity.ToString(),
                Status = alert.status.ToString(),
                StudentId = studentSession.student_id,
                StudentName = studentSession.Student != null
                    ? $"{studentSession.Student.first_name} {studentSession.Student.last_name}".Trim()
                    : "N/A",
                StudentNumber = studentSession.Student?.university_number ?? "N/A",
                SessionId = studentSession.exam_session_id,
                SessionName = studentSession.ExamSession?.title ?? "N/A",
                TriggeredAt = alert.triggered_at
            };

        public async Task<IEnumerable<ActiveSessionDto>> GetActiveSessionsAsync(int? restrictToProctorId = null)
        {
            var sessions = await _sessionRepository.GetByStatusesAsync(
                new[] { ExamSessionStatus.ACTIVE, ExamSessionStatus.GRACE });

            if (restrictToProctorId != null)
            {
                var allowedIds = await _proctorSessionRepository
                    .GetSessionIdsByProctorAsync(restrictToProctorId.Value);

                sessions = sessions.Where(s => allowedIds.Contains(s.id)).ToList();
            }

            return sessions.Select(s => new ActiveSessionDto
            {
                Id = s.id,
                Title = s.title,
                CourseTag = s.course_tag,
                Status = s.status.ToString()
            });
        }

        public async Task<(bool Allowed, IEnumerable<StudentMonitoringDto> Students)> GetSessionStudentsAsync(
            int sessionId,
            int? restrictToProctorId = null)
        {
            if (restrictToProctorId != null)
            {
                var allowedIds = await _proctorSessionRepository
                    .GetSessionIdsByProctorAsync(restrictToProctorId.Value);

                // Checked before touching the session so an unassigned proctor learns
                // nothing about it, not even whether it exists.
                if (!allowedIds.Contains(sessionId))
                    return (false, Array.Empty<StudentMonitoringDto>());
            }

            var studentSessions = await _sessionRepository.GetStudentSessionsWithAlertsAsync(sessionId);

            var students = studentSessions.Select(ss =>
            {
                var openAlerts = ss.Alerts?.Where(a => a.status == AlertStatus.Open).ToList() ?? new();
                var latestAlert = ss.Alerts?
                    .OrderByDescending(a => a.triggered_at)
                    .FirstOrDefault();

                return new StudentMonitoringDto
                {
                    StudentSessionId = ss.id,
                    StudentId = ss.student_id,
                    StudentName = ss.Student != null
                        ? $"{ss.Student.first_name} {ss.Student.last_name}".Trim()
                        : "N/A",
                    StudentNumber = ss.Student?.university_number ?? "N/A",
                    Status = ss.status.ToString(),
                    LoginAt = ss.login_at,
                    OpenAlertCount = openAlerts.Count,
                    LatestAlertType = latestAlert?.alert_type
                };
            }).ToList();

            return (true, students);
        }
    }
}
