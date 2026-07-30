namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public enum EditRestoreSessionResult
    {
        Updated = 1,
        NotFound = 2,
        NotScheduled = 3,
        InvalidProctor = 4,
        ProctorNotAvailable = 5,
        InvalidStudent = 6,
    }
}
