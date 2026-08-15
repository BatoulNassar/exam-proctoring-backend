namespace ExamProctoring.Application.Features.Settings.DTOs
{
    public class SystemSettingsDto
    {
        public int GazeAlertThresholdSec { get; set; }
        public string FaceSensitivity { get; set; }
        public bool AmbientAudioMonitoring { get; set; }
        public int GracePeriodMinutes { get; set; }
        public int LoginWindowMinutes { get; set; }
        public int MaxLivenessAttempts { get; set; }

        /// Legacy percentage. Not a cosine similarity and not used by identity verification.
        public int FaceMatchThreshold { get; set; }

        /// SFace cosine similarity a probe must reach to be MATCHED, 0..1.
        ///
        /// Null means "not configured", which is a real and expected state: the production
        /// value must be calibrated against genuine enrolment and probe samples before it can
        /// be set. Until then identity verification fails closed rather than comparing against
        /// a guessed number.
        public double? SFaceCosineThreshold { get; set; }
        public bool QuestionRandomisation { get; set; }
        public bool OptionShuffle { get; set; }

        /// <summary>Warnings allowed in one session before it is terminated. 0 disables it.</summary>
        public int MaxWarningsBeforeTermination { get; set; }
    }
}
