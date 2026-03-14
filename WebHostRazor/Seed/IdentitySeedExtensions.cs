using Microsoft.AspNetCore.Identity;

namespace WebHostRazor.Seed;

public static class IdentitySeedExtensions
{
    public static async Task SeedIdentityAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        string[] roles = { "Admin", "Host", "Customer" };
        foreach (var r in roles)
            if (!await roleMgr.RoleExistsAsync(r))
                await roleMgr.CreateAsync(new IdentityRole(r));

        // host user
        await EnsureUser(userMgr, "host@demo.com", "Host@123", "Host");
    }

    private static async Task EnsureUser(UserManager<IdentityUser> userMgr, string email, string pass, string role)
    {
        var u = await userMgr.FindByEmailAsync(email);
        if (u == null)
        {
            u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            await userMgr.CreateAsync(u, pass);
        }

        if (!await userMgr.IsInRoleAsync(u, role))
            await userMgr.AddToRoleAsync(u, role);
    }
}
*/