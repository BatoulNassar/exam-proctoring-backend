using System;
using System.Collections.Generic;

namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public class ExamSessionDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CourseTag { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public int GracePeriodMinutes { get; set; }
        public int LoginWindowMinutes { get; set; }

        public IEnumerable<AvailableProctorDto> AssignedProctors { get; set; } = new List<AvailableProctorDto>();
        public IEnumerable<EnrolledStudentDto> EnrolledStudents { get; set; } = new List<EnrolledStudentDto>();

        public int QuestionBankId { get; set; }
        public string QuestionBankTitle { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public bool Randomization { get; set; }
        public bool OptionShuffle { get; set; }

        public int EyeGazeThresholdSec { get; set; }
        public string FaceAlertSensitivity { get; set; } = string.Empty;

        public DateTime? LockedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
