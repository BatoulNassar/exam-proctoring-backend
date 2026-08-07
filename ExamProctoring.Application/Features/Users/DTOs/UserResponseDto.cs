namespace ExamProctoring.Application.Features.Users.DTOs
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public List<string> Permissions { get; set; }
    }
}