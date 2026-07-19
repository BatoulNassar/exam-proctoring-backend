namespace ExamProctoring.Application.Features.AuditLogs.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int ActorId { get; set; }
        public string ActorType { get; set; } = string.Empty; 
        public string Action { get; set; } = string.Empty; 
        public string EntityType { get; set; } = string.Empty; 
        public int EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public int ExamSessionId { get; set; }
    }
}