using ExamProctoring.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;

namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public class CreateExamSessionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        /// <summary>Exam day, as "2026-12-12".</summary>
        public DateOnly ScheduledDate { get; set; }

        /// <summary>Start time on that day in Damascus local time, as "11:30".</summary>
        public TimeOnly ScheduledTime { get; set; }
        public int DurationMinutes { get; set; }
        public int GracePeriodMinutes { get; set; } = 5;
        public int LoginWindowMinutes { get; set; } = 5;
        public int QuestionBankId { get; set; }
        public int EyeGazeThresholdSec { get; set; } = 3;
        public FaceAlertSensitivity FaceAlertSensitivity { get; set; } = FaceAlertSensitivity.Medium;
        public int[]? AssignedProctorIds { get; set; }
        public IFormFile? StudentsCsvFile { get; set; }
        public bool QuestionRandomisation { get; set; } = true;
        public bool OptionShuffle { get; set; } = true;
        public bool AudioMonitoring { get; set; } = false;
    }
}
