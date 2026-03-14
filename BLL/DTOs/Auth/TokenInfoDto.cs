using Microsoft.AspNetCore.Identity;

namespace BLL.DTOs.Auth;

public class TokenInfoDto
{
    public bool IsValid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public IdentityUser? User { get; set; }
}