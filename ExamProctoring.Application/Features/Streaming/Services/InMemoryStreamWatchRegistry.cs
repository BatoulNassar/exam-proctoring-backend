using ExamProctoring.Application.Common.Interfaces;

namespace ExamProctoring.Application.Features.Streaming.Services
{
    /// Process-local active watch table. Single lock keeps multi-key invariants correct
    /// at human-scale watch rates. Hosted as a singleton from the API process.
    public sealed class InMemoryStreamWatchRegistry : IStreamWatchRegistry
    {
        private readonly object _gate = new();
        private readonly Dictionary<Guid, StreamWatchEntry> _byWatchId = new();
        private readonly Dictionary<int, Guid> _watchByStudentSession = new();
        private readonly Dictionary<int, int> _watchCountByExamSession = new();

        public bool TryStart(
            int examSessionId,
            int studentSessionId,
            string proctorConnectionId,
            string studentConnectionId,
            int maxPerSession,
            out StreamWatchEntry entry,
            out string? rejectCode)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(proctorConnectionId);
            ArgumentException.ThrowIfNullOrWhiteSpace(studentConnectionId);

            entry = null!;
            rejectCode = null;

            if (maxPerSession <= 0)
            {
                rejectCode = StreamWatchCodes.Capacity;
                return false;
            }

            lock (_gate)
            {
                if (_watchByStudentSession.ContainsKey(studentSessionId))
                {
                    rejectCode = StreamWatchCodes.WatchInProgress;
                    return false;
                }

                _watchCountByExamSession.TryGetValue(examSessionId, out var count);
                if (count >= maxPerSession)
                {
                    rejectCode = StreamWatchCodes.Capacity;
                    return false;
                }

                var watchId = Guid.NewGuid();
                entry = new StreamWatchEntry(
                    watchId,
                    examSessionId,
                    studentSessionId,
                    proctorConnectionId,
                    studentConnectionId,
                    DateTime.UtcNow);

                _byWatchId[watchId] = entry;
                _watchByStudentSession[studentSessionId] = watchId;
                _watchCountByExamSession[examSessionId] = count + 1;
                return true;
            }
        }

        public bool TryGet(Guid watchId, out StreamWatchEntry entry)
        {
            lock (_gate)
            {
                return _byWatchId.TryGetValue(watchId, out entry!);
            }
        }

        public bool TryRemove(Guid watchId, out StreamWatchEntry entry)
        {
            lock (_gate)
            {
                if (!_byWatchId.TryGetValue(watchId, out entry!))
                    return false;

                RemoveLocked(entry);
                return true;
            }
        }

        public IReadOnlyList<StreamWatchEntry> RemoveAllForConnection(string connectionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
            lock (_gate)
            {
                var matches = _byWatchId.Values
                    .Where(e =>
                        string.Equals(e.ProctorConnectionId, connectionId, StringComparison.Ordinal)
                        || string.Equals(e.StudentConnectionId, connectionId, StringComparison.Ordinal))
                    .ToList();

                foreach (var entry in matches)
                    RemoveLocked(entry);

                return matches;
            }
        }

        public bool TryRemoveForStudentSession(int studentSessionId, out StreamWatchEntry entry)
        {
            lock (_gate)
            {
                if (!_watchByStudentSession.TryGetValue(studentSessionId, out var watchId)
                    || !_byWatchId.TryGetValue(watchId, out entry!))
                {
                    entry = null!;
                    return false;
                }

                RemoveLocked(entry);
                return true;
            }
        }

        private void RemoveLocked(StreamWatchEntry entry)
        {
            _byWatchId.Remove(entry.WatchId);
            _watchByStudentSession.Remove(entry.StudentSessionId);

            if (_watchCountByExamSession.TryGetValue(entry.ExamSessionId, out var count))
            {
                if (count <= 1)
                    _watchCountByExamSession.Remove(entry.ExamSessionId);
                else
                    _watchCountByExamSession[entry.ExamSessionId] = count - 1;
            }
        }
    }
}
