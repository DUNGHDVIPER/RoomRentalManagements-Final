using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DAL.Seed;

public static class IdentitySeeder
{
    public static readonly string[] Roles = ["Admin", "Host", "Customer", "Tenant"];

    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        CancellationToken ct = default)
    {
        foreach (var role in Roles)
        {
            await EnsureRoleAsync(roleManager, role);
        }

        await EnsureUserAsync(userManager, "admin@demo.com", "Admin@123!", "Admin");
        await EnsureUserAsync(userManager, "host@demo.com", "Host@123!", "Host");
        await EnsureUserAsync(userManager, "customer@demo.com", "Customer@123!", "Customer");
        await EnsureUserAsync(userManager, "tenant@demo.com", "Tenant@123!", "Tenant");
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string role)
    {
        if (await roleManager.RoleExistsAsync(role))
            return;

        try
        {
            var created = await roleManager.CreateAsync(new IdentityRole(role));

            if (!created.Succeeded)
            {
                var msg = string.Join(", ", created.Errors.Select(e => $"{e.Code}: {e.Description}"));

                // Nếu role đã tồn tại do app khác vừa seed xong thì bỏ qua
                if (!await roleManager.RoleExistsAsync(role))
                {
                    throw new InvalidOperationException($"Create role '{role}' failed: {msg}");
                }
            }
        }
        catch (DbUpdateException)
        {
            // Trường hợp 2 app cùng tạo 1 role một lúc
            if (!await roleManager.RoleExistsAsync(role))
                throw;
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("RoleNameIndex", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Phòng thêm cho lỗi duplicate role name từ SQL Server
            if (!await roleManager.RoleExistsAsync(role))
                throw;
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            try
            {
                var created = await userManager.CreateAsync(user, password);

                if (!created.Succeeded)
                {
                    // Có thể app khác vừa tạo xong
                    user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        var msg = string.Join(", ", created.Errors.Select(e => $"{e.Code}: {e.Description}"));
                        throw new InvalidOperationException($"Create user '{email}' failed: {msg}");
                    }
                }
            }
            catch (DbUpdateException)
            {
                user = await userManager.FindByEmailAsync(email);
                if (user == null)
                    throw;
            }
        }

        // reset password về đúng demo password
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, password);

        if (!reset.Succeeded)
        {
            var msg = string.Join(", ", reset.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Reset password for '{email}' failed: {msg}");
        }

        // clear lockout
        await userManager.SetLockoutEndDateAsync(user, null);
        user.AccessFailedCount = 0;
        await userManager.UpdateAsync(user);

        // ensure role
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(role))
        {
            var addRole = await userManager.AddToRoleAsync(user, role);
            if (!addRole.Succeeded)
            {
                var rolesAfter = await userManager.GetRolesAsync(user);
                if (!rolesAfter.Contains(role))
                {
                    var msg = string.Join(", ", addRole.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new InvalidOperationException($"Add role '{role}' to '{email}' failed: {msg}");
                }
            }
        }
    }
}