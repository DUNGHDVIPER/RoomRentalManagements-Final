using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAdmin.MVC.Models.Auth;

namespace WebAdmin.MVC.Controllers;

public class AccountController : Controller
{
    private const string LoginViewPath = "~/Views/Auth/Login.cshtml";
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        // ✅ Kiểm tra xem đã authenticated chưa
        if (User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("User already authenticated, redirecting to dashboard");
            return RedirectToAction("Index", "Dashboard");
        }

        // ✅ Kiểm tra cookies từ WebHostRazor
        var authResult = await TryLoginFromCookiesAsync();
        if (authResult.Success)
        {
            _logger.LogInformation("Auto-login successful from cookies for user: {Email}", authResult.Email);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        return View(LoginViewPath, new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(LoginViewPath, vm);

        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", vm.Email);

            // ✅ BƯỚC 1: Thử đăng nhập với Identity Database trước
            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user != null)
            {
                _logger.LogInformation("User found in database: {Email}", vm.Email);

                // Kiểm tra user có bị lock không
                if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning("User {Email} is locked out", vm.Email);
                    vm.ErrorMessage = "Account is locked. Please contact administrator.";
                    return View(LoginViewPath, vm);
                }

                // Thử sign in với SignInManager để đảm bảo authentication đúng cách
                var signInResult = await _signInManager.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: true);

                if (signInResult.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    _logger.LogInformation("Database login successful for {Email} with roles: {Roles}", vm.Email, string.Join(",", roles));

                    // Kiểm tra có quyền Admin không
                    if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
                    {
                        _logger.LogInformation("User {Email} has admin privileges", vm.Email);

                        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                            return Redirect(returnUrl);

                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        _logger.LogWarning("User {Email} logged in but doesn't have admin privileges. Roles: {Roles}", vm.Email, string.Join(",", roles));
                        await _signInManager.SignOutAsync();
                        vm.ErrorMessage = "You don't have permission to access admin area.";
                        return View(LoginViewPath, vm);
                    }
                }
                else if (signInResult.IsLockedOut)
                {
                    _logger.LogWarning("User {Email} account locked out after login attempt", vm.Email);
                    vm.ErrorMessage = "Account is locked due to multiple failed attempts.";
                    return View(LoginViewPath, vm);
                }
                else
                {
                    _logger.LogWarning("Password verification failed for user {Email}", vm.Email);
                    vm.ErrorMessage = "Invalid password.";
                    return View(LoginViewPath, vm);
                }
            }

            // ✅ BƯỚC 2: Fallback về demo accounts nếu không tìm thấy trong database
            _logger.LogInformation("User {Email} not found in database, trying demo accounts", vm.Email);

            string? role = null;

            if (vm.Email.Equals("admin@system.com", StringComparison.OrdinalIgnoreCase) && vm.Password == "Admin@123456")
                role = "Admin";
            else if (vm.Email.Equals("host@demo.com", StringComparison.OrdinalIgnoreCase) && vm.Password == "Host@123")
                role = "Host";

            if (role != null)
            {
                _logger.LogInformation("Demo account login successful for {Email} with role {Role}", vm.Email, role);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, vm.Email),
                    new Claim(ClaimTypes.Name, vm.Email),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("LoginSource", "DemoAccount")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties { IsPersistent = vm.RememberMe });

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Dashboard");
            }

            // ✅ BƯỚC 3: Tất cả đều fail
            _logger.LogWarning("Login failed for {Email} - not found in database or demo accounts", vm.Email);
            vm.ErrorMessage = "Invalid email or password.";
            return View(LoginViewPath, vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", vm.Email);
            vm.ErrorMessage = "An error occurred during login. Please try again.";
            return View(LoginViewPath, vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await _signInManager.SignOutAsync(); // ✅ Thêm SignInManager logout

        // ✅ Xóa cookies từ WebHostRazor
        Response.Cookies.Delete("AuthToken");
        Response.Cookies.Delete("RefreshToken");
        Response.Cookies.Delete("UserId");
        Response.Cookies.Delete("UserEmail");

        _logger.LogInformation("User logged out and cookies cleared");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
        => View("~/Views/Auth/AccessDenied.cshtml");

    // ✅ Improved method để đọc cookies từ WebHostRazor
    private async Task<(bool Success, string? Email, string? Role)> TryLoginFromCookiesAsync()
    {
        try
        {
            // Đọc cookies từ WebHostRazor
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

            // Tìm user trong database
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                _logger.LogWarning("User not found in database: {Email}", userEmail);
                return (false, null, null);
            }

            // Lấy roles của user
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

    // ✅ Debug endpoints
    [HttpGet]
    [AllowAnonymous]
    public IActionResult DebugCookies()
    {
        var cookieInfo = new
        {
            AllCookies = Request.Cookies.Select(c => new { c.Key, HasValue = !string.IsNullOrEmpty(c.Value) }).ToArray(),
            AuthToken = !string.IsNullOrEmpty(Request.Cookies["AuthToken"]),
            UserEmail = Request.Cookies["UserEmail"],
            UserId = Request.Cookies["UserId"],
            RefreshToken = !string.IsNullOrEmpty(Request.Cookies["RefreshToken"]),
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            UserName = User.Identity?.Name,
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
        };

        return Json(cookieInfo);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> TestCookieLogin()
    {
        var result = await TryLoginFromCookiesAsync();
        return Json(new
        {
            Success = result.Success,
            Email = result.Email,
            Role = result.Role,
            IsAuthenticatedAfter = User.Identity?.IsAuthenticated ?? false,
            UserNameAfter = User.Identity?.Name
        });
    }

    // ✅ Method để xử lý JWT token (nếu cần)
    private (bool IsValid, string? Email, IList<string> Roles) ValidateJwtToken(string token)
    {
        try
        {
            // Đây là simple token validation
            // Trong production, bạn nên dùng proper JWT validation
            var decodedBytes = Convert.FromBase64String(token);
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            var parts = decodedString.Split('|');

            if (parts.Length >= 3)
            {
                var email = parts[0];
                var roles = parts[1].Split(',').ToList();
                var expiry = DateTime.ParseExact(parts[2], "yyyy-MM-dd HH:mm:ss", null);

                if (expiry > DateTime.UtcNow)
                {
                    return (true, email, roles);
                }
            }

            return (false, null, new List<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return (false, null, new List<string>());
        }
    }
}