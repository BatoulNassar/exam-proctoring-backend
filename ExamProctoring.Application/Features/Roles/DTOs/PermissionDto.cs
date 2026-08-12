public class PermissionDto
{
    /// <summary>
    /// The value to send back in PUT /api/roles/{roleId}/permissions, which takes
    /// ids. Roles are listed by permission name, so this is the missing link
    /// between what the screen shows and what it has to submit.
    /// </summary>
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
