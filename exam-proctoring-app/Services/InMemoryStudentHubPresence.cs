using ExamProctoring.Application.Common.Interfaces;

namespace ExamProctoring.API.Services
{
    /// Process-local student hub presence + dashboard JoinSession tracking.
    public sealed class InMemoryStudentHubPresence : IStudentHubPresence
    {
        private readonly object _gate = new();
        private readonly Dictionary<int, StudentHubPresenceEntry> _byStudentSession = new();
        private readonly Dictionary<string, int> _studentSessionByConnection = new();
        private readonly Dictionary<string, HashSet<int>> _joinedExamSessionsByConnection = new();

        public void SetStudentConnected(int examSessionId, int studentSessionId, string connectionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
            lock (_gate)
            {
                if (_byStudentSession.TryGetValue(studentSessionId, out var previous)
                    && !string.Equals(previous.ConnectionId, connectionId, StringComparison.Ordinal))
                {
                    _studentSessionByConnection.Remove(previous.ConnectionId);
                }

                var entry = new StudentHubPresenceEntry(
                    examSessionId,
                    studentSessionId,
                    connectionId,
                    DateTime.UtcNow);

                _byStudentSession[studentSessionId] = entry;
                _studentSessionByConnection[connectionId] = studentSessionId;
            }
        }

        public StudentHubPresenceEntry? ClearStudentByConnection(string connectionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
            lock (_gate)
            {
                if (!_studentSessionByConnection.TryGetValue(connectionId, out var studentSessionId))
                    return null;

                _studentSessionByConnection.Remove(connectionId);
                if (_byStudentSession.TryGetValue(studentSessionId, out var entry)
                    && string.Equals(entry.ConnectionId, connectionId, StringComparison.Ordinal))
                {
                    _byStudentSession.Remove(studentSessionId);
                    return entry;
                }

                return null;
            }
        }

        public StudentHubPresenceEntry? ClearStudentBySession(int studentSessionId, string connectionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
            lock (_gate)
            {
                if (!_byStudentSession.TryGetValue(studentSessionId, out var entry))
                    return null;

                if (!string.Equals(entry.ConnectionId, connectionId, StringComparison.Ordinal))
                    return null;

                _byStudentSession.Remove(studentSessionId);
                _studentSessionByConnection.Remove(connectionId);
                return entry;
            }
        }

        public bool TryGetStudentConnectionId(int studentSessionId, out string connectionId)
        {
            lock (_gate)
            {
                if (_byStudentSession.TryGetValue(studentSessionId, out var entry))
                {
                    connectionId = entry.ConnectionId;
                    return true;
                }

                connectionId = string.Empty;
                return false;
            }
        }

        public IReadOnlyList<int> GetConnectedStudentSessionIds(int examSessionId)
        {
            lock (_gate)
            {
                return _byStudentSession.Values
                    .Where(e => e.ExamSessionId == examSessionId)
                    .Select(e => e.StudentSessionId)
                    .OrderBy(id => id)
                    .ToArray();
            }
        }

        public void AddJoinedExamSession(string connectionId, int examSessionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
            lock (_gate)
            {
                if (!_joinedExamSessionsByConnection.TryGetValue(connectionId, out var set))
                {
                    set = new HashSet<int>();
                    _joinedExamSessionsByConnection[connectionId] = set;
                }

                set.Add(examSessionId);
            }
        }

        public void RemoveJoinedExamSession(string connectionId, int examSessionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
            lock (_gate)
            {
                if (!_joinedExamSessionsByConnection.TryGetValue(connectionId, out var set))
                    return;

                set.Remove(examSessionId);
                if (set.Count == 0)
                    _joinedExamSessionsByConnection.Remove(connectionId);
            }
        }

        public bool HasJoinedExamSession(string connectionId, int examSessionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
            lock (_gate)
            {
                return _joinedExamSessionsByConnection.TryGetValue(connectionId, out var set)
                       && set.Contains(examSessionId);
            }
        }

        public void ClearDashboardConnection(string connectionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
            lock (_gate)
            {
                _joinedExamSessionsByConnection.Remove(connectionId);
            }
        }
    }
}
