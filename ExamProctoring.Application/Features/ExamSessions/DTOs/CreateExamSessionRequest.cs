using Microsoft.AspNetCore.Http;

public class CreateExamSessionRequest
{
    public string Title { get; set; }
    public string CourseCode { get; set; }
    public DateTime ScheduledStart { get; set; }
    public int DurationMinutes { get; set; }
    public int GracePeriodMinutes { get; set; } = 5;
    public int LoginWindowMinutes { get; set; }

    public IFormFile QuestionBankFile { get; set; }
    public IFormFile StudentsCsvFile { get; set; }

    public int EyeGazeThresholdSec { get; set; }
    public string FaceAlertSensitivity { get; set; } 
    public bool QuestionRandomisation { get; set; }
    public bool OptionShuffle { get; set; }
    public bool AudioMonitoring { get; set; }

    public int AssignedProctorId { get; set; }
}