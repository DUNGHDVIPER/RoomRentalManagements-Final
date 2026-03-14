namespace BLL.DTOs.Identity;

public class CreateUserDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
