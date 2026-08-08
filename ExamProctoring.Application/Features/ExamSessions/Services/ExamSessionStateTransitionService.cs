using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;

namespace ExamProctoring.Application.Features.ExamSessions.Services
{
    public interface IExamSessionStateTransitionService
    {
        Task<(bool Success, string Message)> PublishSessionAsync(int sessionId, int actorId);
        Task<(bool Success, string Message)> ExtendSessionAsync(int sessionId, int? extendByMinutes, int actorId);
        Task CheckAndUpdateSessionStatesAsync();
    }

    public class ExamSessionStateTransitionService : IExamSessionStateTransitionService
    {
        private readonly IExamSessionRepository _examSessionRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ISystemSettingsRepository _settingsRepository;

        public ExamSessionStateTransitionService(
            IExamSessionRepository examSessionRepository,
            IAuditLogRepository auditLogRepository,
            ISystemSettingsRepository settingsRepository)
        {
            _examSessionRepository = examSessionRepository;
            _auditLogRepository = auditLogRepository;
            _settingsRepository = settingsRepository;
        }

        public async Task<(bool Success, string Message)> PublishSessionAsync(int sessionId, int actorId)
        {
            var session = await _examSessionRepository.GetByIdAsync(sessionId);
            if (session == null)
                return (false, "Session not found");

            if (session.status != ExamSessionStatus.DRAFT)
                return (false, $"Only DRAFT sessions can be published. Current status: {session.status}");

            session.status = ExamSessionStatus.SCHEDULED;
            session.scheduled_at = DateTime.UtcNow;
            session.updated_at = DateTime.UtcNow;
            session.updated_by = actorId;

            await _examSessionRepository.UpdateAsync(session);
            await AddAuditLogAsync(sessionId, actorId, "PublishSession", "Session published");

            return (true, "Session published successfully");
        }

        public async Task<(bool Success, string Message)> ExtendSessionAsync(int sessionId, int? extendByMinutes, int actorId)
        {
            var session = await _examSessionRepository.GetByIdAsync(sessionId);
            if (session == null)
                return (false, "Session not found");

            if (session.status != ExamSessionStatus.ACTIVE)
                return (false, "Can only extend ACTIVE sessions");

            if (!extendByMinutes.HasValue || extendByMinutes <= 0)
            {
                var settings = await _settingsRepository.GetAsync();
                extendByMinutes = settings?.grace_period_minutes ?? 15;
            }

            if (extendByMinutes > 120)
                return (false, "Extension must be between 1 and 120 minutes");

            session.extended_by_minutes += extendByMinutes.Value;
            session.grace_period_minutes = extendByMinutes.Value;
            session.grace_period_ended_at = DateTime.UtcNow.AddMinutes(extendByMinutes.Value);
            session.status = ExamSessionStatus.GRACE;
            session.updated_at = DateTime.UtcNow;
            session.updated_by = actorId;

            await _examSessionRepository.UpdateAsync(session);
            await AddAuditLogAsync(sessionId, actorId, "ExtendSession",
                $"Session extended by {extendByMinutes} minutes");

            return (true, "Session extended successfully");
        }

        public async Task CheckAndUpdateSessionStatesAsync()
        {
            var now = DateTime.UtcNow;

            // SCHEDULED → LOCKED (24 hours before start_time)
            await TransitionToLockedAsync(now);

            // LOCKED → ACTIVE (at start_time)
            await TransitionToActiveAsync(now);

            // ACTIVE → CLOSED (at end_time, if not extended)
            await TransitionToClosedAsync(now);

            // GRACE → CLOSED (when grace period ends)
            await TransitionFromGraceToClosedAsync(now);

            // CLOSED → ARCHIVED (7 days after closed)
            await TransitionToArchivedAsync(now);
        }

        private async Task TransitionToLockedAsync(DateTime now)
        {
            var lockThresholdTime = now.AddHours(24);

            var sessionsToLock = await _examSessionRepository.GetByStatusAsync(
                ExamSessionStatus.SCHEDULED,
                x => x.start_time <= lockThresholdTime);

            foreach (var session in sessionsToLock)
            {
                session.status = ExamSessionStatus.LOCKED;
                session.locked_at = now;
                session.updated_at = now;
                await _examSessionRepository.UpdateAsync(session);
                await AddAuditLogAsync(session.id, 0, "AutoTransition", "Session locked (24 hours before start)");
            }
        }

        private async Task TransitionToActiveAsync(DateTime now)
        {
            var sessionsToActive = await _examSessionRepository.GetByStatusAsync(
                ExamSessionStatus.LOCKED,
                x => now >= x.start_time);

            foreach (var session in sessionsToActive)
            {
                session.status = ExamSessionStatus.ACTIVE;
                session.active_at = now;
                session.updated_at = now;
                await _examSessionRepository.UpdateAsync(session);
                await AddAuditLogAsync(session.id, 0, "AutoTransition", "Session started - moved to ACTIVE");
            }
        }

        private async Task TransitionToClosedAsync(DateTime now)
        {
            // The boundary includes login_window_minutes because a student may legally begin at
            // the very end of the login window and still receives the full duration. Closing at
            // start_time + duration would cut that student's personal deadline short.
            // Canonical definition: ExamSessionTiming.LatestPersonalEndsAt - restated inline
            // because EF Core cannot translate a method call inside a query predicate.
            var sessionsToClose = await _examSessionRepository.GetByStatusAsync(
                ExamSessionStatus.ACTIVE,
                x => now >= x.start_time.AddMinutes(x.login_window_minutes + x.duration_minutes)
                     && x.extended_by_minutes == 0);

            foreach (var session in sessionsToClose)
            {
                session.status = ExamSessionStatus.CLOSED;
                session.closed_at = now;
                session.updated_at = now;
                await _examSessionRepository.UpdateAsync(session);
                await AddAuditLogAsync(session.id, 0, "AutoTransition", "Session ended - moved to CLOSED");
            }
        }

        private async Task TransitionFromGraceToClosedAsync(DateTime now)
        {
            // Same login-window allowance as TransitionToClosedAsync for the fallback branch,
            // so a session with no explicit grace end cannot close before the latest legally
            // started attempt could finish.
            var sessionsToClose = await _examSessionRepository.GetByStatusAsync(
                ExamSessionStatus.GRACE,
                x => (x.grace_period_ended_at.HasValue && now >= x.grace_period_ended_at) ||
                     (!x.grace_period_ended_at.HasValue
                      && now >= x.start_time.AddMinutes(x.login_window_minutes + x.duration_minutes)));

            foreach (var session in sessionsToClose)
            {
                session.status = ExamSessionStatus.CLOSED;
                session.closed_at = now;
                session.updated_at = now;
                await _examSessionRepository.UpdateAsync(session);
                await AddAuditLogAsync(session.id, 0, "AutoTransition", "Grace period ended - moved to CLOSED");
            }
        }

        private async Task TransitionToArchivedAsync(DateTime now)
        {
            var archivedThresholdTime = now.AddDays(-7);

            var sessionsToArchive = await _examSessionRepository.GetByStatusAsync(
                ExamSessionStatus.CLOSED,
                x => x.closed_at.HasValue && x.closed_at <= archivedThresholdTime);

            foreach (var session in sessionsToArchive)
            {
                session.status = ExamSessionStatus.ARCHIVED;
                session.archived_at = now;
                session.updated_at = now;
                await _examSessionRepository.UpdateAsync(session);
                await AddAuditLogAsync(session.id, 0, "AutoTransition", "Session archived (7+ days after closing)");
            }
        }

        private async Task AddAuditLogAsync(int sessionId, int actorId, string action, string details)
        {
            var actorType = actorId == 0 ? "System" : "User";

            await _auditLogRepository.AddAsync(new AuditLog
            {
                exam_session_id = sessionId,
                actor_id = actorId,
                actor_type = actorType,
                action = action,
                entity_type = "ExamSession",
                details = details,
                occurred_at = DateTime.UtcNow,
                created_at = DateTime.UtcNow,
                created_by = actorId
            });
        }
    }
}
