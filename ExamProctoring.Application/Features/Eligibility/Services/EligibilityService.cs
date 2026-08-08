using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Eligibility.DTOs;
using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Eligibility.Services
{
    public class EligibilityService : IEligibilityService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IStudentSessionRepository _studentSessionRepository;
        private readonly ILogger<EligibilityService> _logger;

        /// Exam session states in which a not-yet-started attempt can never begin.
        /// GRACE is reserved for continuing an attempt that already started.
        private static readonly ExamSessionStatus[] HardClosedStates =
        {
            ExamSessionStatus.GRACE,
            ExamSessionStatus.CLOSED,
            ExamSessionStatus.ARCHIVED,
        };

        public EligibilityService(
            IStudentRepository studentRepository,
            IStudentSessionRepository studentSessionRepository,
            ILogger<EligibilityService> logger)
        {
            _studentRepository = studentRepository;
            _studentSessionRepository = studentSessionRepository;
            _logger = logger;
        }

        public async Task<EligibilityResult> GetEligibilityAsync(int studentId)
        {
            // One UTC instant drives every comparison and the reported serverTimeUtc.
            var nowUtc = DateTime.UtcNow;

            // The access token outlives deactivation, so the student is revalidated every request.
            // A deleted student is hidden by the global soft-delete filter and is therefore
            // indistinguishable here from one that never existed - deliberately so.
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || !student.is_active)
            {
                _logger.LogWarning(
                    "Eligibility denied: student {StudentId} is missing, deleted or inactive.", studentId);

                return EligibilityResult.AccountInactive();
            }

            var assignments = await _studentSessionRepository.GetVisibleAssignmentsAsync(studentId);

            if (assignments.Count == 0)
                return Resolved(studentId, nowUtc, null, false, EligibilityReasonCodes.NoSessionAssigned);

            // Tier 1: assignments whose new-start window is open right now.
            var openNow = assignments.Where(a => IsStartWindowOpen(a, nowUtc)).ToList();

            if (openNow.Count > 1)
            {
                _logger.LogWarning(
                    "Eligibility conflict: student {StudentId} has {Count} exam sessions with an open start window.",
                    studentId, openNow.Count);

                return EligibilityResult.MultipleActiveSessions();
            }

            var selected = openNow.Count == 1
                ? openNow[0]
                : SelectNextFuture(assignments, nowUtc) ?? SelectMostRecentPast(assignments);

            if (selected == null)
                return Resolved(studentId, nowUtc, null, false, EligibilityReasonCodes.NoSessionAssigned);

            var (isEligible, reasonCode) = Evaluate(selected, nowUtc);

            return Resolved(studentId, nowUtc, selected, isEligible, reasonCode);
        }

        /// Decides the outcome for the selected assignment. The attempt state is checked before
        /// timing, so a submitted, started or terminated attempt is reported as such regardless
        /// of where the clock currently sits.
        private (bool IsEligible, string? ReasonCode) Evaluate(StudentAssignmentView assignment, DateTime nowUtc)
        {
            if (IsSubmitted(assignment))
                return (false, EligibilityReasonCodes.AlreadySubmitted);

            // A definitively closed session outranks a started attempt: once the exam session is
            // CLOSED or ARCHIVED there is nothing left to resume.
            if (IsDefinitivelyClosed(assignment))
                return (false, EligibilityReasonCodes.SessionClosed);

            if (assignment.StudentSessionStatus == StudentSessionStatus.Terminated)
                return (false, EligibilityReasonCodes.SessionClosed);

            // Started attempts while the session is still live (including GRACE, which the later
            // Resume Exam flow may allow continuation in).
            if (assignment.StudentSessionStatus == StudentSessionStatus.InExam
                || assignment.StudentSessionStatus == StudentSessionStatus.Disconnected)
                return (false, EligibilityReasonCodes.ExamAlreadyStarted);

            // Remaining state is NotStarted: the decision is a timing question.
            // GRACE is not a late-entry period, so a new start is refused.
            if (IsHardClosed(assignment))
                return (false, EligibilityReasonCodes.SessionClosed);

            if (nowUtc < assignment.StartTime)
                return (false, EligibilityReasonCodes.SessionNotStarted);

            return IsStartWindowOpen(assignment, nowUtc)
                ? (true, null)
                : (false, EligibilityReasonCodes.SessionClosed);
        }

        /// The window a new attempt may start in: [start_time, start_time + login_window_minutes)
        /// and still before the exam actually ends. The persisted ExamSession status is not used
        /// as the trigger because the background transition service updates it only periodically;
        /// the hard-closed states still win.
        private static bool IsStartWindowOpen(StudentAssignmentView assignment, DateTime nowUtc)
        {
            if (IsSubmitted(assignment)
                || assignment.StudentSessionStatus == StudentSessionStatus.Terminated
                || IsHardClosed(assignment))
                return false;

            return nowUtc >= assignment.StartTime
                   && nowUtc < LoginWindowClosesAt(assignment)
                   && nowUtc < EndTime(assignment);
        }

        private static StudentAssignmentView? SelectNextFuture(
            IEnumerable<StudentAssignmentView> assignments, DateTime nowUtc) =>
            assignments
                .Where(a => a.StartTime > nowUtc && !IsHardClosed(a))
                .OrderBy(a => a.StartTime)
                .ThenBy(a => a.ExamSessionId)
                .FirstOrDefault();

        private static StudentAssignmentView? SelectMostRecentPast(
            IEnumerable<StudentAssignmentView> assignments) =>
            assignments
                .OrderByDescending(a => a.StartTime)
                .ThenBy(a => a.ExamSessionId)
                .FirstOrDefault();

        private static bool IsSubmitted(StudentAssignmentView assignment) =>
            assignment.StudentSessionStatus == StudentSessionStatus.Submitted
            || assignment.SubmittedAt.HasValue;

        /// Blocks a new start: GRACE, CLOSED and ARCHIVED.
        private static bool IsHardClosed(StudentAssignmentView assignment) =>
            HardClosedStates.Contains(assignment.ExamSessionStatus);

        /// The session is over for every purpose, including continuing an attempt that had started.
        private static bool IsDefinitivelyClosed(StudentAssignmentView assignment) =>
            assignment.ExamSessionStatus == ExamSessionStatus.CLOSED
            || assignment.ExamSessionStatus == ExamSessionStatus.ARCHIVED;

        /// Both boundaries defer to ExamSessionTiming so the attempt and eligibility features
        /// cannot drift apart on the arithmetic. Values are unchanged from before the extraction.
        private static DateTime LoginWindowClosesAt(StudentAssignmentView assignment) =>
            ExamSessionTiming.LoginWindowClosesAt(assignment.StartTime, assignment.LoginWindowMinutes);

        private static DateTime EndTime(StudentAssignmentView assignment) =>
            ExamSessionTiming.CohortWorkEndsAt(
                assignment.StartTime, assignment.DurationMinutes, assignment.ExtendedByMinutes);

        private EligibilityResult Resolved(
            int studentId,
            DateTime nowUtc,
            StudentAssignmentView? assignment,
            bool isEligible,
            string? reasonCode)
        {
            _logger.LogInformation(
                "Eligibility resolved for student {StudentId}: eligible={IsEligible} reason={ReasonCode} examSessionId={ExamSessionId}.",
                studentId, isEligible, reasonCode ?? "(none)", assignment?.ExamSessionId);

            return EligibilityResult.Resolved(new EligibilityResponse
            {
                IsEligible = isEligible,
                ReasonCode = reasonCode,
                ServerTimeUtc = nowUtc,
                Session = assignment == null ? null : MapSession(assignment),
            });
        }

        /// Values stored in datetime2 come back with DateTimeKind.Unspecified; they are stamped
        /// as UTC here so the response serializes with a trailing 'Z'.
        private static EligibleSessionDto MapSession(StudentAssignmentView assignment) =>
            new()
            {
                Id = assignment.ExamSessionId,
                Title = assignment.Title,
                CourseCode = assignment.CourseTag,
                Status = assignment.ExamSessionStatus.ToString(),
                ScheduledStartUtc = AsUtc(assignment.StartTime),
                DurationMinutes = assignment.DurationMinutes,
                EndTimeUtc = AsUtc(EndTime(assignment)),
                LoginWindowClosesAtUtc = AsUtc(LoginWindowClosesAt(assignment)),
                SubmittedAtUtc = assignment.SubmittedAt.HasValue
                    ? AsUtc(assignment.SubmittedAt.Value)
                    : null,
            };

        private static DateTime AsUtc(DateTime value) =>
            value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
