using System.Security.Claims;

namespace WebHostRazor.Security;

public static class UserClaimsExtensions
{
    public static int? GetActorUserId(this ClaimsPrincipal user)
    {
        var s = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(s, out var id) ? id : null;
    }
}