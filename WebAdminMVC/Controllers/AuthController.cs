using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAdmin.MVC.Models.Auth;
using System.Text;

namespace WebAdmin.MVC.Controllers;

public class AuthController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> AdminLogin(string? token = null)
    {
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var decodedBytes = Convert.FromBase64String(token);
                var decodedString = Encoding.UTF8.GetString(decodedBytes);
                var parts = decodedString.Split('|');

                if (parts.Length >= 3)
                {
                    var email = parts[0];
                    var roles = parts[1].Split(',');
                    var expiry = DateTime.ParseExact(parts[2], "yyyy-MM-dd HH:mm:ss", null);

                    if (expiry > DateTime.UtcNow && (roles.Contains("Admin") || roles.Contains("SuperAdmin")))
                    {
                        var user = await _userManager.FindByEmailAsync(email);
                        if (user != null)
                        {
                            var userRoles = await _userManager.GetRolesAsync(user);
                            if (userRoles.Contains("Admin") || userRoles.Contains("SuperAdmin"))
                            {
                                await _signInManager.SignInAsync(user, isPersistent: true);
                                _logger.LogInformation("Admin user {Email} signed in from WebHostRazor", email);
                                return RedirectToAction("Index", "Dashboard");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing admin login token");
            }
        }

        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Admin") && !roles.Contains("SuperAdmin"))
                {
                    model.ErrorMessage = "Access denied. Admin role required.";
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
     user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin user logged in");
                    return LocalRedirect(returnUrl ?? Url.Action("Index", "Dashboard")!);
                }

                if (result.IsLockedOut)
                {
                    model.ErrorMessage = "This account has been locked out.";
                    return View(model);
                }
            }

            model.ErrorMessage = "Invalid login attempt or insufficient permissions.";
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            model.ErrorMessage = "An error occurred during login.";
            return View(model);
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Admin user logged out");
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}