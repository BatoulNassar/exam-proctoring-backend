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
        public int FaceMatchThreshold { get; set; }
        public bool QuestionRandomisation { get; set; }
        public bool OptionShuffle { get; set; }

        /// <summary>Warnings allowed in one session before it is terminated. 0 disables it.</summary>
        public int MaxWarningsBeforeTermination { get; set; }
    }
}
