using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using WebAdminMVC.Models.Roles; // ✅ Thêm using cho models

namespace WebAdminMVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class RolesController(
    RoleManager<IdentityRole> roleManager,
    UserManager<IdentityUser> userManager,
    ILogger<RolesController> logger) : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly ILogger<RolesController> _logger = logger;

    private static readonly string[] SystemRoles = ["SuperAdmin", "Admin", "Host", "Tenant"];

    // GET: /Roles/Index
    public async Task<IActionResult> Index()
    {
        // ✅ Thêm cookie authentication check
        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        try
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var roleVms = new List<RoleListItemVm>();

            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

                roleVms.Add(new RoleListItemVm
                {
                    Id = role.Id,
                    Name = role.Name!,
                    UserCount = usersInRole.Count,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // ✅ Thêm debug info từ cookies
            ViewData["UserEmail"] = GetUserEmailFromCookies();
            ViewData["LoginSource"] = GetLoginSourceFromCookies();

            return View(roleVms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roles");
            TempData["Error"] = "Error loading roles: " + ex.Message;
            return View(new List<RoleListItemVm>());
        }
    }

    // GET: /Roles/Create
    public async Task<IActionResult> Create()
    {
        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        return View(new RoleCreateVm());
    }

    // POST: /Roles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleCreateVm model)
    {
        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var roleExists = await _roleManager.RoleExistsAsync(model.Name);
            if (roleExists)
            {
                ModelState.AddModelError("Name", "Role already exists");
                return View(model);
            }

            var role = new IdentityRole(model.Name);
            var result = await _roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                var cookieEmail = GetUserEmailFromCookies();
                _logger.LogInformation("Created role: {RoleName} by {CreatedBy}", model.Name, cookieEmail ?? User.Identity?.Name);
                TempData["Success"] = "Role created successfully";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {RoleName}", model.Name);
            ModelState.AddModelError(string.Empty, "An error occurred while creating the role");
        }

        return View(model);
    }

    // GET: /Roles/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        var model = new RoleEditVm
        {
            Id = role.Id,
            Name = role.Name!
        };

        return View(model);
    }

    // POST: /Roles/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, RoleEditVm model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
            {
                var cookieEmail = GetUserEmailFromCookies();
                _logger.LogInformation("Updated role: {RoleName} by {UpdatedBy}", model.Name, cookieEmail ?? User.Identity?.Name);
                TempData["Success"] = "Role updated successfully";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {RoleName}", model.Name);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the role");
        }

        return View(model);
    }

    // POST: /Roles/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        try
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                TempData["Error"] = "Role not found";
                return RedirectToAction(nameof(Index));
            }

            // Check if role is being used by any users
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Count != 0)
            {
                // 🔥 Hiển thị thông báo chi tiết với số lượng users
                TempData["Error"] = $"❌ Cannot delete role '{role.Name}' because it is currently assigned to {usersInRole.Count} user(s). Please remove all users from this role before deleting it.";
                _logger.LogWarning("Attempted to delete role {RoleName} but it has {UserCount} users assigned", role.Name, usersInRole.Count);
                return RedirectToAction(nameof(Index));
            }

            // Prevent deleting system roles
            if (SystemRoles.Contains(role.Name))
            {
                TempData["Error"] = $"❌ Cannot delete system role '{role.Name}'. System roles are protected and cannot be removed.";
                _logger.LogWarning("Attempted to delete protected system role: {RoleName}", role.Name);
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                var cookieEmail = GetUserEmailFromCookies();
                _logger.LogInformation("Deleted role: {RoleName} by {DeletedBy}", role.Name, cookieEmail ?? User.Identity?.Name);
                TempData["Success"] = $"✅ Role '{role.Name}' has been successfully deleted.";
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"❌ Error deleting role '{role.Name}': {errors}";
                _logger.LogError("Failed to delete role {RoleName}: {Errors}", role.Name, errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role with id: {RoleId}", id);
            TempData["Error"] = "❌ An unexpected error occurred while deleting the role. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Roles/Details/5
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

        var model = new RoleDetailsVm
        {
            Id = role.Id,
            Name = role.Name!,
            Users = usersInRole.Select(u => new UserInRoleVm
            {
                Id = u.Id,
                Email = u.Email!,
                UserName = u.UserName!
            }).ToList()
        };

        return View(model);
    }

    // ✅ Cookie Authentication Helper Methods
    private async Task<(bool IsAuthenticated, IActionResult RedirectResult)> EnsureAuthenticatedAsync()
    {
        if (User.Identity.IsAuthenticated)
        {
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
            if (userRoles.Any(r => r == "Admin" || r == "SuperAdmin"))
            {
                return (true, null);
            }
            else
            {
                _logger.LogWarning("User authenticated but doesn't have Admin role: {Roles}", string.Join(",", userRoles));
                return (false, RedirectToAction("AccessDenied", "Account"));
            }
        }

        _logger.LogDebug("User not authenticated, trying cookie auto-login");
        var cookieLoginResult = await TryAutoLoginFromCookiesAsync();

        if (cookieLoginResult.Success)
        {
            _logger.LogInformation("Cookie auto-login successful for roles controller");
            return (true, null);
        }

        _logger.LogWarning("Authentication failed, redirecting to login");
        return (false, RedirectToAction("Login", "Account"));
    }

    private async Task<(bool Success, string? Email, string? Role)> TryAutoLoginFromCookiesAsync()
    {
        try
        {
            var authToken = Request.Cookies["AuthToken"];
            var userEmail = Request.Cookies["UserEmail"];
            var userId = Request.Cookies["UserId"];

            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(userId))
            {
                return (false, null, null);
            }

            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                return (false, null, null);
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin") && !roles.Contains("SuperAdmin"))
            {
                return (false, userEmail, null);
            }

            // Sign in user using SignInManager
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roles.Contains("SuperAdmin") ? "SuperAdmin" : "Admin"),
                    new Claim("LoginSource", "CookieAutoLogin")
                }, CookieAuthenticationDefaults.AuthenticationScheme)));

            return (true, userEmail, roles.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cookie auto-login for roles");
            return (false, null, null);
        }
    }

    private string? GetUserEmailFromCookies()
    {
        return Request.Cookies["UserEmail"];
    }

    private string? GetLoginSourceFromCookies()
    {
        return User.Claims.FirstOrDefault(c => c.Type == "LoginSource")?.Value ?? "Unknown";
    }
}