namespace ExamProctoring.Application.Features.Streaming.Services
{
    /// Stable reject codes and watch-end reasons from PROCTORING_REALTIME_CONTRACT §6.3.
    public static class StreamWatchCodes
    {
        public const string NotAuthorized = "NOT_AUTHORIZED";
        public const string StudentOffline = "STUDENT_OFFLINE";
        public const string WatchInProgress = "WATCH_IN_PROGRESS";
        public const string Capacity = "CAPACITY";
        public const string StudentNotInSession = "STUDENT_NOT_IN_SESSION";
        public const string InvalidState = "INVALID_STATE";

        public const string ProctorEnded = "proctor_ended";
        public const string StudentEnded = "student_ended";
        public const string ProctorDisconnected = "proctor_disconnected";
        public const string StudentDisconnected = "student_disconnected";
        public const string ServerShutdown = "server_shutdown";

        public const string DisconnectReasonLeave = "leave";
        public const string DisconnectReasonConnectionLost = "connection_lost";
    }
}
