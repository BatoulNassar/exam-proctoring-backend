using System.Collections.Generic;

namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public class CreateExamSessionResponse
    {
        public ExamSessionDetailsDto Session { get; set; } = null!;
        public int EnrolledStudentsCount { get; set; }
        public List<string> UnmatchedUniversityNumbers { get; set; } = new();
    }
}
