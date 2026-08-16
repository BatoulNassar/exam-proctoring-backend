namespace ExamProctoring.Application.Features.Monitoring.DTOs
{
    /// <summary>
    /// Hub fan-out payload for roster status changes (attempt lifecycle + pipeline flips).
    /// </summary>
    public sealed class StudentStatusChangedDto
    {
        public int StudentSessionId { get; init; }
        public string NewStatus { get; init; } = string.Empty;
        public DateTime? LoginAtUtc { get; init; }
        public string? PipelineStatus { get; init; }
        public DateTime? LastHeartbeatAtUtc { get; init; }
    }
}
