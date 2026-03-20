using DAL.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebCustomer.Blazor.Seed;

public static class IdentitySeedExtensions
{
    public static async Task SeedIdentityAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        // Use AuthDbContext for in-memory store, no migration needed
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        string[] roles = { "Admin", "Host", "Customer" };
        foreach (var r in roles)
        {
            if (!await roleMgr.RoleExistsAsync(r))
            {
                await roleMgr.CreateAsync(new IdentityRole(r));
            }
        }

        // sample users
        await EnsureUser(userMgr, "admin@demo.com", "Admin@123", "Admin");
        await EnsureUser(userMgr, "host@demo.com", "Host@123", "Host");
        await EnsureUser(userMgr, "customer@demo.com", "Customer@123", "Customer");
    }

    private static async Task EnsureUser(UserManager<IdentityUser> userMgr, string email, string pw, string role)
    {
        var u = await userMgr.FindByEmailAsync(email);
        if (u == null)
        {
            u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var cr = await userMgr.CreateAsync(u, pw);
            if (!cr.Succeeded) throw new Exception(string.Join("; ", cr.Errors.Select(e => e.Description)));
        }

        if (!await userMgr.IsInRoleAsync(u, role))
            await userMgr.AddToRoleAsync(u, role);
    }
}
