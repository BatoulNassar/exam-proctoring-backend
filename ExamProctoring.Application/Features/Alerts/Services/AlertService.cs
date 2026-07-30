using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Alerts.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Alerts.Services
{
    public class AlertService : IAlertService
    {
        private readonly IAlertRepository _alertRepository;

        public AlertService(IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }


        public async Task<IEnumerable<AlertEventDto>> GetRecentAlertsAsync(int page, int pageSize)
        {
            var alerts = await _alertRepository.GetRecentPagedAsync(page, pageSize);

            return alerts.Select(alert => new AlertEventDto
            {
                Id = alert.id,
                AlertType = alert.alert_type,
                TriggeredAt = alert.triggered_at,
                DeliveredAt = alert.delivered_at,
            }).ToList();
        }
    }
}