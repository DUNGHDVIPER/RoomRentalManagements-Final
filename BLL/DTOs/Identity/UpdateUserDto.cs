namespace BLL.DTOs.Identity;

public class UpdateUserDto
{
    public string Email { get; set; } = null!;
    public bool IsLocked { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}
