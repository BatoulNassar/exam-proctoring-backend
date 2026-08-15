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

        /// Legacy. Seeded as 95 and read only by the settings screen; no code makes a decision
        /// from it. It predates the choice of SFace and is NOT a cosine similarity - a literal
        /// 0.95 would reject every legitimate student, since genuine SFace pairs score ~0.4-0.7.
        /// Left in place untouched; <see cref="sface_cosine_threshold"/> is what identity
        /// verification actually uses.
        public int face_match_threshold { get; set; }

        /// The SFace cosine similarity a probe must reach to be MATCHED, expressed in the
        /// model's own units (0..1).
        ///
        /// Deliberately nullable and deliberately unseeded: the production value must be
        /// calibrated against real enrolment and probe samples, and shipping a guessed default
        /// would either lock legitimate students out of their exams or admit impersonators.
        /// Until it is configured, identity verification fails closed with SERVER_ERROR rather
        /// than comparing against an invented number.
        public double? sface_cosine_threshold { get; set; }
        public bool question_randomisation { get; set; }
        public bool option_shuffle { get; set; }

        /// <summary>
        /// How many warnings a student may receive in one session before the session
        /// is terminated automatically. Zero disables automatic termination.
        /// </summary>
        public int max_warnings_before_termination { get; set; } = 3;
    }
}
