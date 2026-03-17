using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebAdminMVC.ViewModels.Dashboard;

namespace WebAdminMVC.Controllers;

public class AdminDashboardController : Controller
{
    private readonly IReportService _reportService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(
        IReportService reportService,
        UserManager<IdentityUser> userManager,
        ILogger<AdminDashboardController> logger)
    {
        _reportService = reportService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // ✅ Kiểm tra và auto-login từ cookies nếu chưa authenticate
        if (!User.Identity.IsAuthenticated)
        {
            var cookieLoginResult = await TryAutoLoginFromCookiesAsync();
            if (!cookieLoginResult.Success)
            {
                _logger.LogWarning("No valid authentication found. Redirecting to login.");
                return RedirectToAction("Login", "Account");
            }
        }

        try
        {
            // Lấy dữ liệu dashboard
            var revenueReport = await _reportService.GetRevenueDashboardAsync(DateTime.Now.Year);

            var vm = new DashboardViewModel
            {
                TotalRevenue = revenueReport.TotalRevenue,
                TotalRooms = revenueReport.TotalRooms,
                OccupiedRooms = revenueReport.OccupiedRooms,
                RevenueByMonth = (IReadOnlyList<RevenueByMonthItem>)revenueReport.RevenueByMonth
            };

            // ✅ Thêm thông tin user vào ViewData từ cookies
            ViewData["UserEmail"] = GetUserEmailFromCookies();
            ViewData["LoginSource"] = GetLoginSourceFromCookies();

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            TempData["Error"] = "Unable to load dashboard data. Please try again.";
            return View(new DashboardViewModel());
        }
    }

    // ✅ Method để tự động đăng nhập từ cookies
    private async Task<(bool Success, string? Email, string? Role)> TryAutoLoginFromCookiesAsync()
    {
        try
        {
            // Đọc cookies từ WebHostRazor
            var authToken = Request.Cookies["AuthToken"];
            var userEmail = Request.Cookies["UserEmail"];
            var userId = Request.Cookies["UserId"];
            var refreshToken = Request.Cookies["RefreshToken"];

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
            if (!roles.Contains("Admin") && !roles.Contains("SuperAdmin"))
            {
                _logger.LogWarning("User {Email} does not have Admin role. Roles: {Roles}", userEmail, string.Join(",", roles));
                return (false, userEmail, null);
            }

            var adminRole = roles.Contains("SuperAdmin") ? "SuperAdmin" : "Admin";

            // Tạo claims và sign in
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Email!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, adminRole),
                new Claim("LoginSource", "WebHostRazorCookie"), // ✅ Đánh dấu nguồn login
                new Claim("AuthToken", authToken ?? ""), // ✅ Lưu token vào claim
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                });

            _logger.LogInformation("Successfully auto-logged in user {Email} with role {Role} from cookies", userEmail, adminRole);
            return (true, userEmail, adminRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-login from cookies");
            return (false, null, null);
        }
    }

    // ✅ Method để lấy thông tin từ cookies
    private string? GetUserEmailFromCookies()
    {
        return Request.Cookies["UserEmail"] ?? User.FindFirst(ClaimTypes.Email)?.Value;
    }

    private string? GetLoginSourceFromCookies()
    {
        return User.FindFirst("LoginSource")?.Value ?? "Direct";
    }

    // ✅ Method để kiểm tra cookies có hợp lệ không
    [HttpGet]
    public IActionResult CheckCookieStatus()
    {
        var cookieInfo = new
        {
            AuthToken = !string.IsNullOrEmpty(Request.Cookies["AuthToken"]),
            UserEmail = Request.Cookies["UserEmail"],
            UserId = Request.Cookies["UserId"],
            RefreshToken = !string.IsNullOrEmpty(Request.Cookies["RefreshToken"]),
            IsAuthenticated = User.Identity.IsAuthenticated,
            UserRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray()
        };

        return Json(cookieInfo);
    }

    // ✅ Method để xóa cookies và logout
    [HttpPost]
    public async Task<IActionResult> ClearCookiesAndLogout()
    {
        try
        {
            // Xóa cookies
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax
            };

            Response.Cookies.Append("AuthToken", "", cookieOptions);
            Response.Cookies.Append("RefreshToken", "", cookieOptions);
            Response.Cookies.Append("UserId", "", cookieOptions);
            Response.Cookies.Append("UserEmail", "", cookieOptions);

            // Logout khỏi current session
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("Cookies cleared and user logged out");
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cookies");
            return RedirectToAction("Index");
        }
    }
}