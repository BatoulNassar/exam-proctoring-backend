using ExamProctoring.Domain.Enums;
using System;

namespace ExamProctoring.Application.Features.Eligibility.DTOs
{
    /// Flat read-only projection of one StudentSession joined to its ExamSession.
    /// Carries only the columns the eligibility decision needs; no navigation
    /// collections (answers, alerts, monitoring, proctors, questions) are loaded.
    public class StudentAssignmentView
    {
        public int StudentSessionId { get; set; }
        public StudentSessionStatus StudentSessionStatus { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public int ExamSessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CourseTag { get; set; } = string.Empty;
        public ExamSessionStatus ExamSessionStatus { get; set; }
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public int ExtendedByMinutes { get; set; }
        public int LoginWindowMinutes { get; set; }
    }
}
