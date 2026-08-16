namespace ExamProctoring.Application.Common.Settings
{
    /// Client-side monitoring thresholds returned by Start Exam so one payload configures the
    /// student client.
    ///
    /// TEMPORARY for v1. Only two of the contract's five values have a home in the database
    /// today (ExamSession.eye_gaze_threshold_sec and SystemSettings.ambient_audio_monitoring);
    /// the remaining three live here until the realtime monitoring feature defines where they
    /// belong. Deliberately no database columns were added for them in this phase.
    public class MonitoringPolicySettings
    {
        /// Used only when no SystemSettings row exists, which is the case on any environment
        /// that has not run the development demo seed.
        public bool AudioMonitoringEnabledFallback { get; set; } = true;

        /// Negative decibel threshold, e.g. -40.
        public int AudioNoiseThresholdDb { get; set; } = -40;

        public int HeartbeatIntervalSeconds { get; set; } = 10;

        public int ConnectivityLostThresholdSeconds { get; set; } = 60;
    }
}
