using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.AuditLogs.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.AuditLogs.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _repository;

        public AuditLogService(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<AuditLogDto>> GetRecentAuditsAsync(int page, int pageSize)
        {
            var logs = await _repository.GetRecentPagedAsync(page, pageSize);

            return logs.Select(log => new AuditLogDto
            {
                Id = log.id,
                ActorId = log.actor_id,
                ActorType = log.actor_type,
                Action = log.action,
                EntityId = log.entity_id,
                EntityType = log.entity_type,
                Details = log.details,
                OccurredAt = log.occurred_at,
                ExamSessionId = log.exam_session_id
            }).ToList();
        }
    }
}