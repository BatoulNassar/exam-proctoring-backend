namespace ExamProctoring.Application.Common.Settings
{
    /// WebRTC signaling support for on-demand live watch (PROCTORING_REALTIME_CONTRACT).
    /// Media never touches this process — only ICE configuration and concurrency caps.
    public class WebRtcSettings
    {
        /// At least one STUN URL is required for clients to form peer connections.
        public string[] StunUrls { get; set; } = new[] { "stun:stun.l.google.com:19302" };

        /// Optional TURN URLs for campus NAT. Empty is valid for LAN-only demos.
        public string[] TurnUrls { get; set; } = Array.Empty<string>();

        public string? TurnUsername { get; set; }

        public string? TurnCredential { get; set; }

        /// Soft cap of concurrent watches per exam session (contract default 10).
        public int MaxConcurrentWatchesPerSession { get; set; } = 10;

        /// When set, ICE responses include expiresAtUtc so clients refetch after this many minutes.
        public int? IceCredentialTtlMinutes { get; set; } = 5;
    }

    /// Dashboard origins allowed to negotiate SignalR and call the API from the browser.
    public class CorsSettings
    {
        public string[] AllowedOrigins { get; set; } = new[]
        {
            "http://localhost:5173",
            "http://127.0.0.1:5173",
        };
    }
}
