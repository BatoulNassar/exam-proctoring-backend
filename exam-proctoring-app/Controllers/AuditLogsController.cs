using ExamProctoring.API.Common;
using ExamProctoring.Application.Features.AuditLogs.DTOs;
using ExamProctoring.Application.Features.AuditLogs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace exam_proctoring_app.Controllers 
{
    [Route("api/audits")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetRecentAudits([FromQuery] int page = 1,[FromQuery] int pageSize = 10)
        {
            var audits = await _auditLogService.GetRecentAuditsAsync(page, pageSize);

            return Ok(
                ApiResponse<IEnumerable<AuditLogDto>>.Ok(audits, "Audit logs retrieved successfully")
            );
        }
    }
}