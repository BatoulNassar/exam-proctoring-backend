using ExamProctoring.API.Common;
using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Features.AuditLogs.DTOs;
using ExamProctoring.Application.Features.AuditLogs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text;
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
                ApiResponse<PagedResult<AuditLogDto>>.Ok(audits, $"Retrieved {audits.TotalCount} audit logs")
            );
        }

        /// <summary>
        /// Downloads the whole audit log as CSV. Unlike the per-session export, this
        /// covers every entry, including actions that belong to no exam session.
        /// </summary>
        [HttpGet("export")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ExportAudits()
        {
            var csv = await _auditLogService.ExportCsvAsync();

            var filename = $"audit_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", filename);
        }
    }
}