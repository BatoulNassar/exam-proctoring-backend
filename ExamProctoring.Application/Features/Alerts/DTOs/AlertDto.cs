namespace ExamProctoring.Application.Features.Alerts.DTOs
{
    public class AlertEventDto
    {
        public int Id { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; }

        public string EventName { get; set; } = string.Empty; 
        public string StudentFullName { get; set; } = string.Empty; 

        public bool IsDelivered => DeliveredAt.HasValue;
        public DateTime? DeliveredAt { get; set; }
    }
}