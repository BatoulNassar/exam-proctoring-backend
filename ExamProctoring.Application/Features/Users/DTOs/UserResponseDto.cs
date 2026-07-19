namespace ExamProctoring.Application.Features.Users.DTOs
{
    public class UserResponseDto
    {
        public string UserName { get; set; }
        public List<string> Permissions { get; set; }
    }
}