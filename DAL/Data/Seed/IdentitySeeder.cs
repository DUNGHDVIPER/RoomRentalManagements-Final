using Microsoft.AspNetCore.Identity;

namespace DAL.Seed;

public static class IdentitySeeder
{
    public static readonly string[] Roles = ["Admin", "Host", "Customer", "User"];

    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        CancellationToken ct = default)
    {
        // 1) Roles
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var created = await roleManager.CreateAsync(new IdentityRole(role));
                if (!created.Succeeded)
                {
                    var msg = string.Join("; ", created.Errors.Select(e => $"{e.Code}:{e.Description}"));
                    throw new InvalidOperationException($"Create role '{role}' failed: {msg}");
                }
            }
        }

        // 2) Demo admin & host accounts
        await EnsureUserAsync(userManager, "admin@demo.com", "Admin@123!", "Admin", ct);
        await EnsureUserAsync(userManager, "host@demo.com", "Host@123!", "Host", ct);
        await EnsureUserAsync(userManager, "customer@demo.com", "Customer@123!", "Customer", ct);
        await EnsureUserAsync(userManager, "user@demo.com", "User@123!", "User", ct);
    }

    private static async Task EnsureUserAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string role,
        CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            // Tạo user mới
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var create = await userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                var msg = string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new InvalidOperationException($"Create user '{email}' failed: {msg}");
            }
        }
        // ✅ Bỏ reset password để tránh concurrency conflict

        // ✅ Reload user để đảm bảo có data mới nhất
        user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new InvalidOperationException($"User '{email}' not found after creation");
        }

        // ✅ Logic gán role: nếu chưa có role nào thì gán Customer, nếu có rồi thì gán role chỉ định
        var userRoles = await userManager.GetRolesAsync(user);

        if (userRoles.Count == 0)
        {
            // Chưa có role nào -> gán Customer làm default
            var addCustomerRole = await userManager.AddToRoleAsync(user, "Customer");
            if (!addCustomerRole.Succeeded)
            {
                var msg = string.Join("; ", addCustomerRole.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new InvalidOperationException($"Add default role 'Customer' to '{email}' failed: {msg}");
            }
        }

        // Nếu role chỉ định khác Customer và user chưa có role đó -> thêm role
        if (role != "Customer" && !userRoles.Contains(role))
        {
            var addRole = await userManager.AddToRoleAsync(user, role);
            if (!addRole.Succeeded)
            {
                var msg = string.Join("; ", addRole.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new InvalidOperationException($"Add role '{role}' to '{email}' failed: {msg}");
            }
        }
    }
}