namespace ExamProctoring.Application.Features.Users.DTOs
{
 public class CreateAdminResponseDto
 {
 public int UserId { get; set; }
 public string UserName { get; set; }
 public string FullName { get; set; }
 public string Email { get; set; }
 public string TemporaryPassword { get; set; }
 public List<string> AssignedRoles { get; set; } = new();

 }
}
