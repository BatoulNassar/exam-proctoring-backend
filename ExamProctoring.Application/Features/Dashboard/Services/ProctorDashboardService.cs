using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Alerts;
using ExamProctoring.Application.Features.Dashboard.DTOs;
using ExamProctoring.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Dashboard.Services
{
    public class ProctorDashboardService : IProctorDashboardService
    {
        private const int MaxDays = 90;

        private readonly IProctorSessionRepository _proctorSessionRepository;
        private readonly IDashboardRepository _dashboardRepository;

        public ProctorDashboardService(IProctorSessionRepository proctorSessionRepository, IDashboardRepository dashboardRepository)
        {
            _proctorSessionRepository = proctorSessionRepository;
            _dashboardRepository = dashboardRepository;
        }

        public async Task<ProctorSummaryCardsDto> GetSummaryCardsAsync(int proctorId)
        {
            var sessionIds = await _proctorSessionRepository.GetSessionIdsByProctorAsync(proctorId);

            // A proctor with no assignments sees zeros, never the whole university.
            if (sessionIds.Count == 0)
                return new ProctorSummaryCardsDto();

            return new ProctorSummaryCardsDto
            {
                ActiveSessions = await _dashboardRepository.CountActiveSessionsAmongAsync(sessionIds),
                OpenAlerts = await _dashboardRepository.CountAlertsInSessionsAsync(sessionIds, AlertStatus.Open, null),
                ResolvedAlertsToday = await _dashboardRepository.CountAlertsInSessionsAsync(sessionIds, AlertStatus.Resolved, DateTime.UtcNow.Date)
            };
        }

        public async Task<IReadOnlyList<AlertTypeCountDto>> GetAlertCountsByTypeAsync(int proctorId, int days)
        {
            var sessionIds = await _proctorSessionRepository.GetSessionIdsByProctorAsync(proctorId);

            var clamped = days < 1 ? 1 : (days > MaxDays ? MaxDays : days);
            var from = DateTime.UtcNow.Date.AddDays(-(clamped - 1));

            var counts = await _dashboardRepository.GetAlertCountsByTypeAsync(from, sessionIds);

            // Same zero-filled shape as the admin chart, so the two render identically.
            return AlertTypeCatalog.All
                .Select(type => new AlertTypeCountDto
                {
                    Code = type.Code,
                    Label = type.Label,
                    Count = counts.TryGetValue(type.Code, out var count) ? count : 0
                })
                .ToList();
        }
    }
}
