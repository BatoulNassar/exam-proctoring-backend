using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;

namespace ExamProctoring.Domain.Entities
{
    public class SystemSettings : BaseEntity
    {
        public int gaze_alert_threshold_sec { get; set; }
        public FaceSensitivity face_sensitivity { get; set; }
        public bool ambient_audio_monitoring { get; set; }
        public int grace_period_minutes { get; set; }
        public int login_window_minutes { get; set; }
        public int max_liveness_attempts { get; set; }
        public int face_match_threshold { get; set; }
        public bool question_randomisation { get; set; }
        public bool option_shuffle { get; set; }

        /// <summary>
        /// How many warnings a student may receive in one session before the session
        /// is terminated automatically. Zero disables automatic termination.
        /// </summary>
        public int max_warnings_before_termination { get; set; } = 3;
    }
}
