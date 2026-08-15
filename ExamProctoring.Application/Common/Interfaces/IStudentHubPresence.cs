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
        DateTime ConnectedAtUtc);
}
