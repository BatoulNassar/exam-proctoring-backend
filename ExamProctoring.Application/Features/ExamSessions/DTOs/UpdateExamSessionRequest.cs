using ExamProctoring.Domain.Enums;
using System;

namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    /// <summary>
    /// Partial update: only the fields that are sent (non-null) are applied;
    /// omitted fields keep their current values.
    /// </summary>
    public class UpdateExamSessionRequest
    {
        public string? Title { get; set; }
        public string? CourseTag { get; set; }
        /// <summary>
        /// New exam day, as "2026-12-12". Send together with <see cref="StartTimeOfDay"/>;
        /// one without the other is rejected.
        /// </summary>
        public DateOnly? StartDate { get; set; }

        /// <summary>New start time in Damascus local time, as "11:30".</summary>
        public TimeOnly? StartTimeOfDay { get; set; }
        public int? DurationMinutes { get; set; }
        public int? QuestionBankId { get; set; }
        public int? GracePeriodMinutes { get; set; }
        public int? LoginWindowMinutes { get; set; }
        public int? EyeGazeThresholdSec { get; set; }
        public FaceAlertSensitivity? FaceAlertSensitivity { get; set; }

        /// <summary>
        /// Optional: update assigned proctors (for SCHEDULED state: edit restore)
        /// </summary>
        public int[]? AssignedProctorIds { get; set; }
    }
}
