using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebAdmin.MVC.Models.Users;

namespace WebAdmin.MVC.Controllers;

public class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // ✅ Kiểm tra authentication và thực hiện cookie login nếu cần
        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        try
        {
            var users = await _userManager.Users.ToListAsync();
            var userVms = new List<UserListItemVm>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLocked = await _userManager.IsLockedOutAsync(user);

                userVms.Add(new UserListItemVm
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "No Role",
                    Status = isLocked ? "Locked" : "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                });
            }

            // ✅ Thêm debug info
            ViewData["UserEmail"] = GetUserEmailFromCookies();
            ViewData["LoginSource"] = GetLoginSourceFromCookies();
            ViewData["CookieDebugInfo"] = GetCookieDebugInfo();

            return View(userVms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
            return View(new List<UserListItemVm>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        try
        {
            // ✅ Chỉ lấy roles Admin, Host, Customer - loại bỏ User và Tenant
            ViewBag.Roles = await GetAdminSelectableRolesAsync();
            return View(new UserCreateVm());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create user page");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateVm model)
    {
        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        await LogCookieInfoAsync("Create User POST");

        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await GetAdminSelectableRolesAsync();
            return View(model);
        }

        try
        {
            // ✅ Kiểm tra role có hợp lệ không (không cho phép User và Tenant)
            var allowedRoles = await GetAdminSelectableRolesAsync();
            if (!allowedRoles.Contains(model.Role))
            {
                ModelState.AddModelError("Role", "Invalid role selected.");
                ViewBag.Roles = allowedRoles;
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.Roles = await GetAdminSelectableRolesAsync();
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            if (!model.IsActive)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }

            var cookieEmail = GetUserEmailFromCookies();
            _logger.LogInformation("User {Email} created with role {Role} by {CreatedBy} (from cookie)",
                model.Email, model.Role, cookieEmail ?? User.Identity.Name);

            TempData["SuccessMessage"] = "User created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            ModelState.AddModelError("", "An error occurred while creating the user.");
            ViewBag.Roles = await GetAdminSelectableRolesAsync();
            return View(model);
        }
    }

    [HttpGet]
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

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = await _userManager.IsLockedOutAsync(user);

            var model = new UserEditVm
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? "Customer",
                IsActive = !isLocked
            };

            // ✅ Chỉ cho phép edit các roles được phép
            ViewBag.Roles = await GetAdminSelectableRolesAsync();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user for edit");
            TempData["ErrorMessage"] = "An error occurred while loading the user.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ✅ Method chính để đảm bảo authentication với SignInManager
    private async Task<(bool IsAuthenticated, IActionResult RedirectResult)> EnsureAuthenticatedAsync()
    {
        // Nếu đã authenticated, kiểm tra role
        if (User.Identity.IsAuthenticated)
        {
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
            if (userRoles.Any(r => r == "Admin" || r == "SuperAdmin"))
            {
                _logger.LogDebug("User already authenticated with Admin role");
                return (true, null);
            }
            else
            {
                _logger.LogWarning("User authenticated but doesn't have Admin role: {Roles}", string.Join(",", userRoles));
                return (false, RedirectToAction("AccessDenied", "Account"));
            }
        }

        // Thử auto-login từ cookies
        _logger.LogDebug("User not authenticated, trying cookie auto-login");
        var cookieLoginResult = await TryAutoLoginFromCookiesAsync();

        if (cookieLoginResult.Success)
        {
            _logger.LogInformation("Cookie auto-login successful for {Email}", cookieLoginResult.Email);
            return (true, null);
        }

        _logger.LogWarning("Cookie auto-login failed, redirecting to login");
        return (false, RedirectToAction("Login", "Account"));
    }

    // ✅ Improved cookie login method sử dụng SignInManager
    private async Task<(bool Success, string? Email, string? Role)> TryAutoLoginFromCookiesAsync()
    {
        try
        {
            var authToken = Request.Cookies["AuthToken"];
            var userEmail = Request.Cookies["UserEmail"];
            var userId = Request.Cookies["UserId"];

            _logger.LogDebug("Checking cookies - AuthToken: {HasToken}, Email: {Email}, UserId: {UserId}",
                !string.IsNullOrEmpty(authToken), userEmail, userId);

            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(userId))
            {
                _logger.LogDebug("Missing required cookies from WebHostRazor");
                return (false, null, null);
            }

            _logger.LogInformation("Found cookies from WebHostRazor - Email: {Email}, UserId: {UserId}", userEmail, userId);

            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                _logger.LogWarning("User not found in database: {Email}", userEmail);
                return (false, null, null);
            }

            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogDebug("User {Email} has roles: {Roles}", userEmail, string.Join(",", roles));

            if (!roles.Contains("Admin") && !roles.Contains("SuperAdmin"))
            {
                _logger.LogWarning("User {Email} does not have Admin role. Roles: {Roles}", userEmail, string.Join(",", roles));
                return (false, userEmail, null);
            }

            // ✅ Sử dụng SignInManager thay vì manual cookie authentication
            await _signInManager.SignInAsync(user, isPersistent: true);

            var adminRole = roles.Contains("SuperAdmin") ? "SuperAdmin" : "Admin";
            _logger.LogInformation("Successfully auto-logged in user {Email} with role {Role} from cookies using SignInManager", userEmail, adminRole);

            return (true, userEmail, adminRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-login from cookies");
            return (false, null, null);
        }
    }

    // ✅ Method mới để lấy roles được phép tạo/edit trong Admin panel
    private async Task<List<string>> GetAdminSelectableRolesAsync()
    {
        try
        {
            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

            // ✅ Chỉ cho phép Admin tạo các roles: Admin, Host, Customer
            // Loại bỏ: User, Tenant, SuperAdmin (SuperAdmin chỉ có thể tạo bằng code/migration)
            var allowedRoles = allRoles.Where(role =>
                role == "Admin" ||
                role == "Host" ||
                role == "Customer"
            ).ToList();

            _logger.LogDebug("Admin selectable roles: {Roles}", string.Join(",", allowedRoles));
            return allowedRoles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin selectable roles");
            // ✅ Fallback với roles được phép
            return new List<string> { "Admin", "Host", "Customer" };
        }
    }

    private async Task<List<string>> GetRolesAsync()
    {
        try
        {
            return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roles");
            return new List<string> { "Admin", "Host", "Customer" };
        }
    }

    private string? GetUserEmailFromCookies()
    {
        return Request.Cookies["UserEmail"] ?? User.FindFirst(ClaimTypes.Email)?.Value;
    }

    private string? GetLoginSourceFromCookies()
    {
        return User.FindFirst("LoginSource")?.Value ?? "Direct";
    }

    private object GetCookieDebugInfo()
    {
        return new
        {
            HasAuthToken = !string.IsNullOrEmpty(Request.Cookies["AuthToken"]),
            UserEmail = Request.Cookies["UserEmail"],
            UserId = Request.Cookies["UserId"],
            HasRefreshToken = !string.IsNullOrEmpty(Request.Cookies["RefreshToken"]),
            IsAuthenticated = User.Identity.IsAuthenticated,
            LoginSource = GetLoginSourceFromCookies(),
            UserClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray(),
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task LogCookieInfoAsync(string action)
    {
        var cookieInfo = GetCookieDebugInfo();
        _logger.LogInformation("Action: {Action} - Cookie Info: {@CookieInfo}", action, cookieInfo);
    }

    // ✅ Debug endpoint để test cookies
    [HttpGet]
    [AllowAnonymous]
    public IActionResult CheckCookieStatus()
    {
        var cookieInfo = GetCookieDebugInfo();
        _logger.LogInformation("Cookie status check: {@CookieInfo}", cookieInfo);
        return Json(cookieInfo);
    }

    // ✅ Test endpoint để force cookie login
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> TestCookieLogin()
    {
        var result = await TryAutoLoginFromCookiesAsync();
        return Json(new
        {
            Success = result.Success,
            Email = result.Email,
            Role = result.Role,
            IsAuthenticated = User.Identity.IsAuthenticated,
            CookieInfo = GetCookieDebugInfo()
        });
    }

    // ✅ Additional methods để complete controller
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditVm model)
    {
        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await GetAdminSelectableRolesAsync();
            return View(model);
        }

        try
        {
            // ✅ Kiểm tra role có hợp lệ không
            var allowedRoles = await GetAdminSelectableRolesAsync();
            if (!allowedRoles.Contains(model.Role))
            {
                ModelState.AddModelError("Role", "Invalid role selected.");
                ViewBag.Roles = allowedRoles;
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Update basic user properties
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.Roles = await GetAdminSelectableRolesAsync();
                return View(model);
            }

            // Update user role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, model.Role);

            // Update lockout status
            if (model.IsActive)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }

            var cookieEmail = GetUserEmailFromCookies();
            _logger.LogInformation("User {Email} updated by {UpdatedBy} (from cookie)",
                model.Email, cookieEmail ?? User.Identity.Name);

            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            ModelState.AddModelError("", "An error occurred while updating the user.");
            ViewBag.Roles = await GetAdminSelectableRolesAsync();
            return View(model);
        }
    }

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
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var cookieEmail = GetUserEmailFromCookies();
            _logger.LogInformation("Attempting to delete user {Email} by {DeletedBy} (from cookie)",
                user.Email, cookieEmail ?? User.Identity.Name);

            var result = await _userManager.DeleteAsync(user);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "User deleted successfully!" : "Failed to delete user.";

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} successfully deleted by {DeletedBy}",
                    user.Email, cookieEmail ?? User.Identity.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            TempData["ErrorMessage"] = "An error occurred while deleting the user.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(string id)
    {
        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        if (string.IsNullOrEmpty(id))
        {
            TempData["ErrorMessage"] = "Invalid user ID.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user is already locked
            var isAlreadyLocked = await _userManager.IsLockedOutAsync(user);
            if (isAlreadyLocked)
            {
                TempData["ErrorMessage"] = "User is already locked.";
                return RedirectToAction(nameof(Index));
            }

            // Lock the user by setting lockout end date to maximum value
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            
            if (result.Succeeded)
            {
                var cookieEmail = GetUserEmailFromCookies();
                _logger.LogInformation("User {Email} locked by {LockedBy} (from cookie)",
                    user.Email, cookieEmail ?? User.Identity.Name);

                TempData["SuccessMessage"] = $"User {user.Email} has been locked successfully.";
            }
            else
            {
                _logger.LogWarning("Failed to lock user {Email}: {Errors}",
                    user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                TempData["ErrorMessage"] = "Failed to lock user: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user with ID: {UserId}", id);
            TempData["ErrorMessage"] = "An error occurred while locking the user.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var authResult = await EnsureAuthenticatedAsync();
        if (!authResult.IsAuthenticated)
        {
            return authResult.RedirectResult;
        }

        if (string.IsNullOrEmpty(id))
        {
            TempData["ErrorMessage"] = "Invalid user ID.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user is actually locked
            var isLocked = await _userManager.IsLockedOutAsync(user);
            if (!isLocked)
            {
                TempData["ErrorMessage"] = "User is not currently locked.";
                return RedirectToAction(nameof(Index));
            }

            // Unlock the user by setting lockout end date to null
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            
            if (result.Succeeded)
            {
                // Reset access failed count to clear any failed login attempts
                await _userManager.ResetAccessFailedCountAsync(user);

                var cookieEmail = GetUserEmailFromCookies();
                _logger.LogInformation("User {Email} unlocked by {UnlockedBy} (from cookie)",
                    user.Email, cookieEmail ?? User.Identity.Name);

                TempData["SuccessMessage"] = $"User {user.Email} has been unlocked successfully.";
            }
            else
            {
                _logger.LogWarning("Failed to unlock user {Email}: {Errors}",
                    user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                TempData["ErrorMessage"] = "Failed to unlock user: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user with ID: {UserId}", id);
            TempData["ErrorMessage"] = "An error occurred while unlocking the user.";
        }

        return RedirectToAction(nameof(Index));
    }
}