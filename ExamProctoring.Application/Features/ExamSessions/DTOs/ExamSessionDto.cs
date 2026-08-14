public class ExamSessionDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string CourseTag { get; set; }
    public string Status { get; set; }
    /// <summary>
    /// Start of the exam, carrying Damascus's offset — "2026-08-14T20:20:00+03:00".
    /// Reads as the wall clock the admin chose while still naming one exact instant.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public int QuestionBankId { get; set; }
    public string QuestionBankName { get; set; }
    public DateTime? LockedAt { get; set; }
    public int GracePeriodMinutes { get; set; }
    public int LoginWindowMinutes { get; set; }
    public int EyeGazeThresholdSec { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public int StudentCount { get; set; }
    public string ProctorName { get; set; }
}