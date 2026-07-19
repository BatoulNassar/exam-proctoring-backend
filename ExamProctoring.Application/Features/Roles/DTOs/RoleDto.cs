public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<string> Permissions { get; set; } = new();
}