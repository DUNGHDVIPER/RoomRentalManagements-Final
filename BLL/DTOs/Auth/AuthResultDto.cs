namespace BLL.DTOs.Auth;

public class AuthResultDto
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}