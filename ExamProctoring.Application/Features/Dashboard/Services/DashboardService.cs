using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Alerts;
using ExamProctoring.Application.Features.Dashboard.DTOs;
using ExamProctoring.Application.Interfaces;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Dashboard.Services
{
    public class DashboardService : IDashboardService
    {
        /// <summary>Widest range a chart may ask for, to keep one request from scanning years of rows.</summary>
        private const int MaxDays = 90;

        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IQuestionBankRepository questionBankRepository, IUserRepository userRepository, IDashboardRepository dashboardRepository)
        {
            _questionBankRepository = questionBankRepository;
            _userRepository = userRepository;
            _dashboardRepository = dashboardRepository;
        }

        // adminId == null means "no owner filter": only a SuperAdmin gets that.
        // Session-derived numbers below all carry the scope; the question-bank and
        // student rosters are shared university-wide and stay unscoped.

        public async Task<DashboardStatsDto> GetStatsAsync(int? adminId)
        {
            return new DashboardStatsDto
            {
                ActiveSessions = await _dashboardRepository.CountActiveSessionsAsync(adminId),
                StudentsInExam = await _dashboardRepository.CountStudentsInExamAsync(adminId),
                OpenAlerts = await _dashboardRepository.CountAlertsByStatusAndSeverityAsync(AlertStatus.Open, null, adminId),
                QuestionBanks = await _questionBankRepository.CountAsync(),
                AdminUsers = await _userRepository.CountAdminsAsync(),
            };
        }

        public async Task<DashboardSummaryCardsDto> GetSummaryCardsAsync(int? adminId)
        {
            // The "Ready to Export" card and the export-status chart read the same
            // query, so the two views can never show contradicting numbers.
            var exportCounts = await _dashboardRepository.GetClosedSessionExportCountsAsync(adminId);

            return new DashboardSummaryCardsDto
            {
                TotalSessions = await _dashboardRepository.CountAllSessionsAsync(adminId),
                // Reuses the existing count so this card and /stats stay in step.
                ActiveSessions = await _dashboardRepository.CountActiveSessionsAsync(adminId),
                RegisteredStudents = await _dashboardRepository.CountRegisteredStudentsAsync(),
                QuestionBanks = await _questionBankRepository.CountAsync(),
                OpenAlerts = await _dashboardRepository.CountAlertsByStatusAndSeverityAsync(AlertStatus.Open, null, adminId),
                CriticalOpenAlerts = await _dashboardRepository.CountAlertsByStatusAndSeverityAsync(AlertStatus.Open, AlertSeverity.Critical, adminId),
                EscalatedAlerts = await _dashboardRepository.CountAlertsByStatusAndSeverityAsync(AlertStatus.Escalated, null, adminId),
                ReadyToExport = exportCounts.Pending
            };
        }

        public async Task<IReadOnlyList<AlertTypeCountDto>> GetAlertCountsByTypeAsync(int days, int? adminId)
        {
            var from = DateTime.UtcNow.Date.AddDays(-(ClampDays(days) - 1));
            var counts = await _dashboardRepository.GetAlertCountsByTypeAsync(from, null, adminId);

            // Driven by the catalog, not by what the database happens to contain, so
            // a type with no alerts still shows as a zero bar instead of vanishing.
            return AlertTypeCatalog.All
                .Select(type => new AlertTypeCountDto
                {
                    Code = type.Code,
                    Label = type.Label,
                    Count = counts.TryGetValue(type.Code, out var count) ? count : 0
                })
                .ToList();
        }

        public async Task<IReadOnlyList<SessionCountByDayDto>> GetSessionCountsByDayAsync(int days, int? adminId)
        {
            var clamped = ClampDays(days);
            var today = DateTime.UtcNow.Date;
            var from = today.AddDays(-(clamped - 1));
            var toExclusive = today.AddDays(1);

            var counts = await _dashboardRepository.GetSessionCountsByDayAsync(from, toExclusive, adminId);
            var byDate = counts.ToDictionary(c => c.Date);

            // Emit every day in the range, including empty ones, so the chart keeps a
            // continuous axis instead of skipping days that had no sessions.
            return Enumerable.Range(0, clamped)
                .Select(offset =>
                {
                    var date = from.AddDays(offset);

                    return byDate.TryGetValue(date, out var found)
                        ? found
                        : new SessionCountByDayDto
                        {
                            Date = date,
                            DayName = date.DayOfWeek.ToString(),
                            SessionCount = 0,
                            StudentCount = 0
                        };
                })
                .ToList();
        }

        public Task<SessionExportStatusDto> GetSessionCountsByExportStatusAsync(int? adminId) =>
            _dashboardRepository.GetClosedSessionExportCountsAsync(adminId);

        public Task<IReadOnlyList<QuestionCountByBankDto>> GetQuestionCountsByBankAsync() =>
            _dashboardRepository.GetQuestionCountsByBankAsync();

        private static int ClampDays(int days) =>
            days < 1 ? 1 : (days > MaxDays ? MaxDays : days);
    }
}
