using System;
using System.Text.Json.Serialization;

namespace ExamProctoring.Application.Features.Eligibility.DTOs
{
    /// The exam session the eligibility decision refers to. All timestamps are UTC.
    public class EligibleSessionDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        /// Maps from ExamSession.course_tag; the QuestionBank is deliberately not joined.
        public string CourseCode { get; set; } = string.Empty;

        /// Persisted ExamSessionStatus name. It can lag behind real time because the
        /// background transition service runs periodically; isEligible and reasonCode
        /// are the authoritative values for the client.
        public string Status { get; set; } = string.Empty;

        public DateTime ScheduledStartUtc { get; set; }

        public int DurationMinutes { get; set; }

        /// start_time + duration_minutes + extended_by_minutes.
        public DateTime EndTimeUtc { get; set; }

        /// start_time + login_window_minutes. A not-yet-started attempt may begin
        /// from ScheduledStartUtc up to, but not including, this instant.
        public DateTime LoginWindowClosesAtUtc { get; set; }

        /// Present only when the attempt actually carries a submission timestamp;
        /// omitted from JSON otherwise rather than sent as null.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? SubmittedAtUtc { get; set; }
    }
}
