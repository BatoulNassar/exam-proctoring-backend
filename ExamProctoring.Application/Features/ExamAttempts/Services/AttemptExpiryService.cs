using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    public interface IAttemptExpiryService
    {
        /// Finalises attempts still running past their personal deadline.
        /// Returns how many were finalised by this pass.
        Task<int> FinaliseExpiredAttemptsAsync();
    }

    /// The janitor for attempts nobody submitted.
    ///
    /// Deliberately NOT the authorization mechanism. PUT Answer checks ends_at directly on every
    /// request, so the few minutes between passes can never let a late answer through - this pass
    /// only tidies up the terminal state and produces the receipt. Getting that the wrong way
    /// round would make the deadline depend on a timer, which is exactly the mistake the
    /// request-time check exists to prevent.
    public class AttemptExpiryService : IAttemptExpiryService
    {
        /// Bounded so one pass cannot hold a connection for an unbounded time if a large cohort
        /// expires simultaneously; the next pass picks up the remainder.
        private const int BatchSize = 200;

        /// Automatic finalisation has no human actor. Matches the convention already used by
        /// ExamSessionStateTransitionService for its own automatic transitions.
        private const int SystemActorId = 0;
        private const string SystemActorType = "System";

        private readonly IAttemptFinalisationRepository _finalisationRepository;
        private readonly IAttemptFinalisationService _finalisationService;
        private readonly IMonitoringNotifier _notifier;
        private readonly ILogger<AttemptExpiryService> _logger;

        public AttemptExpiryService(
            IAttemptFinalisationRepository finalisationRepository,
            IAttemptFinalisationService finalisationService,
            IMonitoringNotifier notifier,
            ILogger<AttemptExpiryService> logger)
        {
            _finalisationRepository = finalisationRepository;
            _finalisationService = finalisationService;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<int> FinaliseExpiredAttemptsAsync()
        {
            var nowUtc = DateTime.UtcNow;

            var expired = await _finalisationRepository.GetExpiredAttemptsAsync(nowUtc, BatchSize);
            if (expired.Count == 0)
                return 0;

            var finalised = 0;

            foreach (var attempt in expired)
            {
                try
                {
                    // Goes through the one shared path, so an expired attempt is frozen by exactly
                    // the same rules as a student submission. If a student submits in the same
                    // instant, one of the two claims the transition and the other observes it.
                    var outcome = await _finalisationService.FinaliseAsync(new AttemptFinalisationContext
                    {
                        StudentSessionId = attempt.StudentSessionId,
                        ExamSessionId = attempt.ExamSessionId,
                        CourseTag = attempt.CourseTag,
                        QuestionCount = attempt.QuestionCount,
                        Reason = AttemptFinalisationReason.ServerExpiry,
                        ActorId = SystemActorId,
                        ActorType = SystemActorType,
                    });

                    if (outcome.Status != AttemptFinalisationStatus.Finalised)
                        continue;

                    finalised++;

                    // After the commit, never before: telling a dashboard an attempt has ended
                    // and then rolling back would be worse than telling it late.
                    await _notifier.NotifyStudentStatusChangedAsync(
                        attempt.ExamSessionId, attempt.StudentSessionId, outcome.Snapshot.Status.ToString());
                }
                catch (Exception ex)
                {
                    // One problematic attempt must not stop the rest of the batch, and the next
                    // pass will retry it.
                    _logger.LogError(ex,
                        "Failed to auto-finalise expired attempt {StudentSessionId}.", attempt.StudentSessionId);
                }
            }

            if (finalised > 0)
                _logger.LogInformation("Auto-finalised {Count} expired attempt(s).", finalised);

            return finalised;
        }
    }
}
