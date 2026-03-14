namespace BLL.DTOs.Identity;

public class UserDto
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public bool IsLocked { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}
