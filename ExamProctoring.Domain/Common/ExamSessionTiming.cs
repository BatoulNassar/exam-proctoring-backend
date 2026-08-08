using System;

namespace ExamProctoring.Domain.Common
{
    /// Single definition of every exam-session time boundary.
    ///
    /// Takes primitives rather than an ExamSession so that both the entity-based services and
    /// the flat read projections (which never load the entity) can share one formula instead
    /// of restating the arithmetic. Timing is always computed from these values rather than
    /// read from the persisted ExamSession.status, because the background transition service
    /// only refreshes that status every few minutes.
    ///
    /// EF Core cannot translate a method call inside a query predicate, so the SQL-side
    /// predicates in ExamSessionStateTransitionService inline the same arithmetic. These
    /// methods remain the canonical definition; keep the two in step.
    public static class ExamSessionTiming
    {
        /// Last instant a not-yet-started attempt may begin: [start_time, +login_window_minutes).
        public static DateTime LoginWindowClosesAt(DateTime startTime, int loginWindowMinutes) =>
            startTime.AddMinutes(loginWindowMinutes);

        /// The cohort's own scheduled work end, ignoring per-student timers.
        /// This is the value the eligibility response reports as the session end time.
        public static DateTime CohortWorkEndsAt(DateTime startTime, int durationMinutes, int extendedByMinutes) =>
            startTime.AddMinutes(durationMinutes + extendedByMinutes);

        /// The latest instant any legally-started attempt could still be working, i.e. a
        /// student who started at the very end of the login window and received the full
        /// duration. The cohort must not be closed before this, or that student's personal
        /// deadline would be cut short by the shared lifecycle.
        public static DateTime LatestPersonalEndsAt(
            DateTime startTime, int loginWindowMinutes, int durationMinutes, int extendedByMinutes) =>
            startTime.AddMinutes(loginWindowMinutes + durationMinutes + extendedByMinutes);

        /// One student's absolute deadline. A late start still receives the full duration,
        /// so this is anchored to when that student actually began, not to the cohort schedule.
        public static DateTime PersonalEndsAt(DateTime startedAtUtc, int durationMinutes) =>
            startedAtUtc.AddMinutes(durationMinutes);

        /// True while a not-yet-started attempt may still begin.
        public static bool IsLoginWindowOpen(DateTime nowUtc, DateTime startTime, int loginWindowMinutes) =>
            nowUtc >= startTime && nowUtc < LoginWindowClosesAt(startTime, loginWindowMinutes);
    }
}
