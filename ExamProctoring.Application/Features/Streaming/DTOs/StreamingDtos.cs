namespace ExamProctoring.Application.Features.Streaming.DTOs
{
    public sealed class StreamWatchRequestedDto
    {
        public Guid WatchId { get; init; }
        public int ExamSessionId { get; init; }
        public int StudentSessionId { get; init; }
        public DateTime RequestedAtUtc { get; init; }
    }

    public sealed class StreamSdpDto
    {
        public Guid WatchId { get; init; }
        public string Sdp { get; init; } = string.Empty;
        public string SdpType { get; init; } = string.Empty;
    }

    public sealed class StreamIceCandidateDto
    {
        public Guid WatchId { get; init; }
        public string? Candidate { get; init; }
        public string? SdpMid { get; init; }
        public int? SdpMLineIndex { get; init; }
    }

    public sealed class StreamWatchEndedDto
    {
        public Guid WatchId { get; init; }
        public int StudentSessionId { get; init; }
        public string Reason { get; init; } = string.Empty;
        public DateTime EndedAtUtc { get; init; }
    }

    public sealed class StreamWatchRejectedDto
    {
        public int StudentSessionId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string? Message { get; init; }
    }

    public sealed class StudentHubConnectedDto
    {
        public int StudentSessionId { get; init; }
        public DateTime ConnectedAtUtc { get; init; }
    }

    public sealed class StudentHubDisconnectedDto
    {
        public int StudentSessionId { get; init; }
        public DateTime DisconnectedAtUtc { get; init; }
        public string Reason { get; init; } = string.Empty;
    }

    public sealed class StudentHubPresenceSnapshotDto
    {
        public int ExamSessionId { get; init; }
        public IReadOnlyList<int> ConnectedStudentSessionIds { get; init; } = Array.Empty<int>();
    }

    public sealed class RequestStreamWatchResultDto
    {
        public Guid WatchId { get; init; }
    }

    public sealed class IceServersResponseDto
    {
        public IReadOnlyList<IceServerEntryDto> IceServers { get; init; } = Array.Empty<IceServerEntryDto>();
        public DateTime? ExpiresAtUtc { get; init; }
    }

    public sealed class IceServerEntryDto
    {
        public IReadOnlyList<string> Urls { get; init; } = Array.Empty<string>();
        public string? Username { get; init; }
        public string? Credential { get; init; }
    }
}
