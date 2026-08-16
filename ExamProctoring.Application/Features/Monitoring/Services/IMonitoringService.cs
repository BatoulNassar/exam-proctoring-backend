using ExamProctoring.Application.Features.Monitoring.DTOs;

namespace ExamProctoring.Application.Features.Monitoring.Services
{
    public interface IMonitoringService
    {
        /// <param name="restrictToProctorId">
        /// When set, only sessions that proctor is assigned to are returned.
        /// Null means unrestricted (admins and super admins).
        /// </param>
        Task<IEnumerable<ActiveSessionDto>> GetActiveSessionsAsync(int? restrictToProctorId = null);

        /// <summary>
        /// Students of one session. Returns Allowed = false when the caller is a
        /// proctor not assigned to this session, so the controller can answer 403.
        /// An empty list would be indistinguishable from a session that genuinely
        /// has no students, which would leak whether the session exists.
        /// </summary>
        Task<(bool Allowed, IEnumerable<StudentMonitoringDto> Students)> GetSessionStudentsAsync(
            int sessionId,
            int? restrictToProctorId = null);

        /// <summary>
        /// Records a detection reported by the student desktop client and raises an
        /// alert for the proctors when the type warrants one.
        /// </summary>
        /// <param name="studentId">Taken from the student's token, never the request.</param>
        /// <param name="rawIdempotencyKey">Optional Idempotency-Key header value.</param>
        Task<ReportMonitoringEventResult> ReportEventAsync(
            ReportMonitoringEventRequest request,
            int studentId,
            string? rawIdempotencyKey = null);
    }
}
