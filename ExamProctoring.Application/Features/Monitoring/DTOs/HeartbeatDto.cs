namespace ExamProctoring.Application.Features.Monitoring.DTOs
{
    /// <summary>
    /// Student → hub pipeline-alive signal (PROCTORING_REALTIME_CONTRACT §10).
    /// </summary>
    public sealed class HeartbeatDto
    {
        public int StudentSessionId { get; set; }
        public DateTime ClientUtc { get; set; }
        /// <summary>OK | CAMERA_DOWN | ENGINE_DEGRADED</summary>
        public string PipelineStatus { get; set; } = "OK";
    }

    public static class PipelineStatuses
    {
        public const string Ok = "OK";
        public const string CameraDown = "CAMERA_DOWN";
        public const string EngineDegraded = "ENGINE_DEGRADED";

        public static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
        {
            Ok,
            CameraDown,
            EngineDegraded,
        };

        public static bool IsKnown(string? status) =>
            !string.IsNullOrWhiteSpace(status) && Allowed.Contains(status);
    }
}
