using ExamProctoring.Application.Features.Dashboard.DTOs;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// <summary>
    /// Aggregate (read-only) queries that exist purely to feed the dashboard.
    /// Kept separate from the per-aggregate repositories so counting logic for
    /// charts does not leak into them.
    /// </summary>
    /// <remarks>
    /// Every method that touches exam sessions takes an <c>adminId</c> owner
    /// scope: <c>null</c> means "no owner filter" (SuperAdmin sees the whole
    /// university), a value means "only sessions created by that admin". The
    /// caller derives it from the token, never from the request.
    /// </remarks>
    public interface IDashboardRepository
    {
        Task<int> CountAllSessionsAsync(int? adminId = null);

        /// <summary>Sessions currently ACTIVE, within the owner scope.</summary>
        Task<int> CountActiveSessionsAsync(int? adminId = null);

        /// <summary>Students currently sitting an exam, within the owner scope.</summary>
        Task<int> CountStudentsInExamAsync(int? adminId = null);

        Task<int> CountRegisteredStudentsAsync();

        Task<int> CountAlertsByStatusAndSeverityAsync(AlertStatus status, AlertSeverity? severity, int? adminId = null);

        /// <summary>
        /// Closed sessions split by whether a grading export already exists.
        /// Single source of truth for both the "Ready to Export" card and the
        /// export-status chart, so the two can never disagree.
        /// </summary>
        Task<SessionExportStatusDto> GetClosedSessionExportCountsAsync(int? adminId = null);

        /// <summary>
        /// Alert counts per alert_type for alerts triggered at or after
        /// <paramref name="from"/>. Types with no alerts are absent; the caller
        /// fills them in as zero.
        /// </summary>
        /// <param name="sessionIds">
        /// When given, only alerts from these exam sessions are counted. Used to
        /// keep a proctor's chart inside the sessions they are assigned to.
        /// </param>
        Task<IReadOnlyDictionary<string, int>> GetAlertCountsByTypeAsync(DateTime from, IReadOnlyCollection<int> sessionIds = null, int? adminId = null);

        /// <summary>
        /// Counts alerts inside the given exam sessions, optionally limited to
        /// those whose last state change happened at or after <paramref name="since"/>.
        /// </summary>
        Task<int> CountAlertsInSessionsAsync(IReadOnlyCollection<int> sessionIds, AlertStatus status, DateTime? since);

        /// <summary>Number of the given sessions that are currently ACTIVE or in GRACE.</summary>
        Task<int> CountActiveSessionsAmongAsync(IReadOnlyCollection<int> sessionIds);

        /// <summary>
        /// Session and student counts per day for sessions starting in
        /// [from, toExclusive). Days with no sessions are absent; the caller
        /// fills them in as zero.
        /// </summary>
        Task<IReadOnlyList<SessionCountByDayDto>> GetSessionCountsByDayAsync(DateTime from, DateTime toExclusive, int? adminId = null);

        Task<IReadOnlyList<QuestionCountByBankDto>> GetQuestionCountsByBankAsync();
    }
}
