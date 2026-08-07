using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.AuditLogs.DTOs;
using System.Text;
using ExamProctoring.Domain.Entities;
using System;
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

        public async Task LogAsync(int examSessionId, int actorId, string actorType, string action, int entityId, string entityType, string details)
        {
            var now = DateTime.UtcNow;

            await _repository.AddDeferredAsync(new AuditLog
            {
                exam_session_id = examSessionId,
                actor_id = actorId,
                actor_type = actorType,
                action = action,
                entity_id = entityId,
                entity_type = entityType,
                details = details,
                occurred_at = now,
                created_at = now,
                created_by = actorId
            });
        }

        public async Task<PagedResult<AuditLogDto>> GetRecentAuditsAsync(int page, int pageSize)
        {
            var logs = await _repository.GetRecentPagedAsync(page, pageSize);
            var totalCount = await _repository.CountAsync();

            var actorIds = logs
                .Select(l => l.actor_id)
                .Where(id => id > 0)
                .Distinct();

            var actorNames = await _repository.GetActorNamesByIdsAsync(actorIds);

            var items = logs.Select(log => new AuditLogDto
            {
                Id = log.id,
                ActorId = log.actor_id,
                ActorName = actorNames.GetValueOrDefault(log.actor_id, string.Empty),
                ActorType = log.actor_type,
                Action = log.action,
                EntityId = log.entity_id,
                EntityType = log.entity_type,
                EntityName = log.entity_type == "ExamSession"
                    ? (log.ExamSession?.title ?? $"ExamSession #{log.entity_id}")
                    : $"{log.entity_type} #{log.entity_id}",
                Details = log.details,
                OccurredAt = log.occurred_at,
                ExamSessionId = log.exam_session_id
            }).ToList();

            return new PagedResult<AuditLogDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<string> ExportCsvAsync()
        {
            var logs = await _repository.GetAllForExportAsync();

            var actorIds = logs.Select(l => l.actor_id).Where(id => id > 0).Distinct();
            var actorNames = await _repository.GetActorNamesByIdsAsync(actorIds);

            var csv = new StringBuilder();

            csv.AppendLine("Audit Log Export");
            csv.AppendLine($"Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine($"Entries: {logs.Count}");
            csv.AppendLine();

            csv.AppendLine("Timestamp (UTC),Actor,Actor Type,Action,Entity Type,Entity Id,Session,Details");

            foreach (var log in logs)
            {
                // The name, not just the id: an export is read by people, and an
                // actor id alone forces the reader back into the database.
                var actor = actorNames.GetValueOrDefault(log.actor_id, $"User #{log.actor_id}");
                var session = log.ExamSession?.title ?? string.Empty;

                csv.AppendLine(string.Join(",",
                    log.occurred_at.ToString("yyyy-MM-dd HH:mm:ss"),
                    CsvEscape(actor),
                    CsvEscape(log.actor_type),
                    CsvEscape(log.action),
                    CsvEscape(log.entity_type),
                    log.entity_id.ToString(),
                    CsvEscape(session),
                    CsvEscape(log.details ?? string.Empty)));
            }

            return csv.ToString();
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
        }
    }
}