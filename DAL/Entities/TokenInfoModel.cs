using Microsoft.AspNetCore.Identity;

namespace BLL.Models;

public class TokenInfoModel
{
    public bool IsValid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public IdentityUser? User { get; set; }
}