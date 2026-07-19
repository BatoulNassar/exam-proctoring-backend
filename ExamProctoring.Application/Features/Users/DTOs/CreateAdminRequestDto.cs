public class CreateAdminRequestDto
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public List<int> RoleIds { get; set; } = new(); 
}