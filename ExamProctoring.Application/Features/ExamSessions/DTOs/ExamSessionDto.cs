public class ExamSessionDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string CourseTag { get; set; }
    public string Status { get; set; }
    public DateTime StartTime { get; set; }
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
}