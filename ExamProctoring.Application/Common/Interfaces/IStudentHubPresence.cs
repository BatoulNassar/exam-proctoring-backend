namespace ExamProctoring.Application.Common.Interfaces
{
    /// Ephemeral SignalR presence for students who have joined their student-session group.
    /// Process-local only — not durable across API instances.
    public interface IStudentHubPresence
    {
        void SetStudentConnected(int examSessionId, int studentSessionId, string connectionId);

        /// Clears student presence for this connection. Returns the cleared entry when found.
        StudentHubPresenceEntry? ClearStudentByConnection(string connectionId);

        /// Clears student presence when the student explicitly leaves (connection may still be open).
        StudentHubPresenceEntry? ClearStudentBySession(int studentSessionId, string connectionId);

        bool TryGetStudentConnectionId(int studentSessionId, out string connectionId);

        IReadOnlyList<int> GetConnectedStudentSessionIds(int examSessionId);

        /// <summary>
        /// Records a pipeline heartbeat. Returns false when the student is not present or the
        /// connection does not own the session. <paramref name="pipelineChanged"/> is true when
        /// the new status differs from the previous non-null value.
        /// </summary>
        bool TryRecordHeartbeat(
            int studentSessionId,
            string connectionId,
            string pipelineStatus,
            DateTime serverUtc,
            out bool pipelineChanged,
            out StudentHubPresenceEntry? entry);

        bool TryGetPresence(int studentSessionId, out StudentHubPresenceEntry entry);

        void AddJoinedExamSession(string connectionId, int examSessionId);

        void RemoveJoinedExamSession(string connectionId, int examSessionId);

        bool HasJoinedExamSession(string connectionId, int examSessionId);

        /// Removes all dashboard join tracking for a dropped connection.
        void ClearDashboardConnection(string connectionId);
    }

    public sealed record StudentHubPresenceEntry(
        int ExamSessionId,
        int StudentSessionId,
        string ConnectionId,
        DateTime ConnectedAtUtc,
        DateTime? LastHeartbeatAtUtc = null,
        string? PipelineStatus = null);
}
