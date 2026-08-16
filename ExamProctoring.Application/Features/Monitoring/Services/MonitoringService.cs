using ExamProctoring.Application.Common;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Alerts;
using ExamProctoring.Application.Features.Alerts.DTOs;
using ExamProctoring.Application.Features.Monitoring.DTOs;
using ExamProctoring.Application.Features.Students.Services;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExamProctoring.Application.Features.Monitoring.Services
{
    public class MonitoringService : IMonitoringService
    {
        public const string MonitoringEventEndpoint = "POST_MONITORING_EVENT";
        public const int MaxDetailsLength = 1000;
        public const int MaxSnapshotDecodedBytes = 400 * 1024;

        private static readonly HashSet<string> AllowedSnapshotContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly IExamSessionRepository _sessionRepository;
        private readonly IProctorSessionRepository _proctorSessionRepository;
        private readonly IStudentSessionRepository _studentSessionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMonitoringNotifier _notifier;
        private readonly ICloudinaryService _cloudinary;
        private readonly IStudentHubPresence _presence;
        private readonly IIdempotencyRepository _idempotencyRepository;
        private readonly ILogger<MonitoringService> _logger;

        public MonitoringService(
            IExamSessionRepository sessionRepository,
            IProctorSessionRepository proctorSessionRepository,
            IStudentSessionRepository studentSessionRepository,
            IUnitOfWork unitOfWork,
            IMonitoringNotifier notifier,
            ICloudinaryService cloudinary,
            IStudentHubPresence presence,
            IIdempotencyRepository idempotencyRepository,
            ILogger<MonitoringService> logger)
        {
            _sessionRepository = sessionRepository;
            _proctorSessionRepository = proctorSessionRepository;
            _studentSessionRepository = studentSessionRepository;
            _unitOfWork = unitOfWork;
            _notifier = notifier;
            _cloudinary = cloudinary;
            _presence = presence;
            _idempotencyRepository = idempotencyRepository;
            _logger = logger;
        }

        public async Task<ReportMonitoringEventResult> ReportEventAsync(
            ReportMonitoringEventRequest request,
            int studentId,
            string? rawIdempotencyKey = null)
        {
            if (!AlertTypeCatalog.IsKnown(request.EventType))
                return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.UnknownEventType };

            if (string.IsNullOrWhiteSpace(request.Details) || request.Details.Length > MaxDetailsLength)
                return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.ValidationError };

            var contentType = string.IsNullOrWhiteSpace(request.SnapshotContentType)
                ? "image/jpeg"
                : request.SnapshotContentType.Trim();

            byte[]? snapshotBytes = null;
            if (!string.IsNullOrWhiteSpace(request.SnapshotBase64))
            {
                if (!AllowedSnapshotContentTypes.Contains(contentType))
                    return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.ValidationError };

                try
                {
                    snapshotBytes = Convert.FromBase64String(request.SnapshotBase64);
                }
                catch (FormatException)
                {
                    return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.ValidationError };
                }

                if (snapshotBytes.Length > MaxSnapshotDecodedBytes)
                    return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.SnapshotTooLarge };
            }

            var studentSession = await _studentSessionRepository.GetByIdAsync(request.StudentSessionId);

            if (studentSession == null)
                return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.SessionNotFound };

            if (studentSession.student_id != studentId)
                return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.NotYourSession };

            if (studentSession.status != StudentSessionStatus.InExam)
                return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.SessionNotActive };

            string? normalisedKey = null;
            IdempotencyRequest? idempotentRequest = null;
            if (!string.IsNullOrWhiteSpace(rawIdempotencyKey))
            {
                if (!IdempotencyKey.TryNormalise(rawIdempotencyKey, out normalisedKey))
                    return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.ValidationError };

                idempotentRequest = new IdempotencyRequest
                {
                    Endpoint = MonitoringEventEndpoint,
                    ResourceKey = request.StudentSessionId.ToString(),
                    RequestHash = HashReportRequest(request, snapshotBytes?.Length ?? 0),
                    ResponseStatus = 200,
                    NowUtc = DateTime.UtcNow,
                };

                var existing = await _idempotencyRepository.FindAsync(
                    request.StudentSessionId, normalisedKey, idempotentRequest);

                if (existing.Outcome == IdempotencyOutcome.Replay)
                    return DeserializeResult(existing.ReplayedBody!);

                if (existing.Outcome == IdempotencyOutcome.Conflict)
                    return new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.IdempotencyConflict };
            }

            var now = DateTime.UtcNow;
            var occurredAt = request.OccurredAt.HasValue && request.OccurredAt.Value <= now
                ? DateTime.SpecifyKind(request.OccurredAt.Value, DateTimeKind.Utc)
                : now;

            string? snapshotUrl = null;
            if (snapshotBytes != null)
            {
                var extension = contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";
                await using var stream = new MemoryStream(snapshotBytes);
                var (ok, url, error) = await _cloudinary.UploadImageAsync(
                    stream,
                    $"alert-{request.StudentSessionId}-{now:yyyyMMddHHmmss}.{extension}",
                    CloudinaryService.AlertSnapshotsFolder);

                if (ok)
                {
                    snapshotUrl = url;
                }
                else
                {
                    _logger.LogWarning(
                        "Alert snapshot upload failed for studentSession {StudentSessionId}: {Error}",
                        request.StudentSessionId, error);
                }
            }

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
                severity = AlertTypeCatalog.GetSeverity(request.EventType),
                status = AlertStatus.Open,
                triggered_at = occurredAt,
                delivered_at = now,
                created_at = now,
                snapshot_url = snapshotUrl,
            };

            await _studentSessionRepository.AddAlertAsync(alert);
            await _unitOfWork.SaveChangesAsync();

            await _notifier.NotifyAlertCreatedAsync(
                studentSession.exam_session_id, MapToAlertDto(alert, studentSession));

            var result = new ReportMonitoringEventResult
            {
                Outcome = ReportMonitoringEventOutcome.AlertRaised,
                MonitoringEventId = monitoringEvent.id,
                AlertId = alert.id,
                SnapshotUrl = snapshotUrl,
            };

            if (normalisedKey != null && idempotentRequest != null)
            {
                idempotentRequest.ResponseBody = JsonSerializer.Serialize(result, JsonOptions);
                await _idempotencyRepository.StoreAsync(
                    request.StudentSessionId, normalisedKey, idempotentRequest);
            }

            return result;
        }

        private static byte[] HashReportRequest(ReportMonitoringEventRequest request, int snapshotByteLength)
        {
            var canonical =
                $"{request.EventType}\n{request.Details}\n{request.OccurredAt?.ToUniversalTime():O}\n{snapshotByteLength}";
            return SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        }

        private static ReportMonitoringEventResult DeserializeResult(string body) =>
            JsonSerializer.Deserialize<ReportMonitoringEventResult>(body, JsonOptions)
            ?? new ReportMonitoringEventResult { Outcome = ReportMonitoringEventOutcome.AlertRaised };

        public static AlertEventDto MapToAlertDto(AlertEvent alert, StudentSession studentSession) =>
            new()
            {
                Id = alert.id,
                AlertType = alert.alert_type,
                AlertDescription = AlertTypeCatalog.GetDescription(alert.alert_type),
                Severity = alert.severity.ToString(),
                Status = alert.status.ToString(),
                StudentId = studentSession.student_id,
                StudentSessionId = studentSession.id,
                StudentName = studentSession.Student != null
                    ? $"{studentSession.Student.first_name} {studentSession.Student.last_name}".Trim()
                    : "N/A",
                StudentNumber = studentSession.Student?.university_number ?? "N/A",
                SessionId = studentSession.exam_session_id,
                SessionName = studentSession.ExamSession?.title ?? "N/A",
                TriggeredAt = alert.triggered_at,
                SnapshotUrl = alert.snapshot_url,
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

                string? pipelineStatus = null;
                DateTime? lastHeartbeat = null;
                if (_presence.TryGetPresence(ss.id, out var presence))
                {
                    pipelineStatus = presence.PipelineStatus;
                    lastHeartbeat = presence.LastHeartbeatAtUtc;
                }

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
                    LatestAlertType = latestAlert?.alert_type,
                    PipelineStatus = pipelineStatus,
                    LastHeartbeatAtUtc = lastHeartbeat,
                };
            }).ToList();

            return (true, students);
        }
    }
}
