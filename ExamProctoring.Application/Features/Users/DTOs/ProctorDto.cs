namespace ExamProctoring.Application.Features.Users.DTOs
{
    public class ProctorDto
    {
        public int ProctorId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int AssignedSessionsCount { get; set; }
        public int ActiveSessionsCount { get; set; }
    }
}
