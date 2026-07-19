using ExamProctoring.Application.Features.Alerts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.Alerts.Services
{
     public interface IAlertService
    {
       public Task<IEnumerable<AlertEventDto>> GetRecentAlertsAsync(int page, int pageSize);
    }
}
