namespace ExamProctoring.Application.Common.Interfaces
{
    /// In-memory registry of active on-demand watches. No EF persistence.
    public interface IStreamWatchRegistry
    {
        /// Attempts to start a watch. On failure, <paramref name="rejectCode"/> is a
        /// <c>StreamWatchCodes</c> reject value (never an end-reason).
        bool TryStart(
            int examSessionId,
            int studentSessionId,
            string proctorConnectionId,
            string studentConnectionId,
            int maxPerSession,
            out StreamWatchEntry entry,
            out string? rejectCode);

        bool TryGet(Guid watchId, out StreamWatchEntry entry);

        bool TryRemove(Guid watchId, out StreamWatchEntry entry);

        /// Removes every watch that references this SignalR connection (proctor or student).
        IReadOnlyList<StreamWatchEntry> RemoveAllForConnection(string connectionId);

        /// Removes the active watch for a student session, if any.
        bool TryRemoveForStudentSession(int studentSessionId, out StreamWatchEntry entry);
    }

    public sealed record StreamWatchEntry(
        Guid WatchId,
        int ExamSessionId,
        int StudentSessionId,
        string ProctorConnectionId,
        string StudentConnectionId,
        DateTime StartedAtUtc);
}
