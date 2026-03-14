using System.Security.Claims;

namespace WebHostRazor.Common;

public static class ClaimsPrincipalExtensions
{
    public static int? GetActorUserId(this ClaimsPrincipal user)
    {
        // tuỳ hệ login của bạn: NameIdentifier hoặc "UserId"
        var s = user.FindFirstValue(ClaimTypes.NameIdentifier)
             ?? user.FindFirstValue("UserId");

        return int.TryParse(s, out var id) ? id : (int?)null;
    }
}