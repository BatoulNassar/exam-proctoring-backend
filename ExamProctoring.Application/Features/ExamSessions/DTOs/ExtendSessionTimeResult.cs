namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public enum ExtendSessionTimeResult
    {
        Extended = 1,
        NotFound = 2,
        NotActive = 3,
        InvalidExtraMinutes = 4,
    }
}
