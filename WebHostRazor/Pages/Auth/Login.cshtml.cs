using BLL.DTOs.Auth;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace WebHostRazor.Pages.Auth;

public class LoginModel(
    IAuthService authService,
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    ITokenService tokenService,
    IConfiguration configuration,
    ILogger<LoginModel> logger) : PageModel
{
    private readonly IAuthService _authService = authService;
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly SignInManager<IdentityUser> _signInManager = signInManager;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<LoginModel> _logger = logger;

    [BindProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; }

    public string? Error { get; set; }
    public string? ReturnUrl { get; set; }

    public string CustomerPortalUrl =>
        $"{(_configuration["CustomerUrl"] ?? "https://localhost:7292").TrimEnd('/')}/login";

    public async Task OnGetAsync(string? returnUrl = null, string? error = null)
    {
        ReturnUrl = returnUrl;
        Error = error;

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var loginRequest = new LoginRequestDto
            {
                Email = Email,
                Password = Password,
                RememberMe = RememberMe
            };

            var result = await _authService.LoginAsync(loginRequest);

            if (!result.Succeeded)
            {
                Error = result.Error ?? "Invalid login attempt.";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                Error = "User not found after login.";
                return Page();
            }

            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("User {Email} logged in with roles: {Roles}", Email, string.Join(",", roles));

            // Customer/Tenant/User KHÔNG đăng nhập ở Host portal nữa
            if (roles.Contains("Customer") || roles.Contains("Tenant") || roles.Contains("User"))
            {
                await ClearHostAuthenticationAsync();

                Error = "Tài khoản Customer/Tenant vui lòng đăng nhập trực tiếp tại Customer Portal.";
                return Page();
            }

            return await RedirectBasedOnRole(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", Email);
            Error = "An error occurred during login. Please try again.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostExternalLoginAsync(string provider, string? returnUrl = null)
    {
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        var redirectUrl = Url.Page("./Login", pageHandler: "ExternalLoginCallback", values: new { returnUrl });
        _logger.LogInformation("Starting external login with {Provider}, callback URL: {RedirectUrl}", provider, redirectUrl);

        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        properties.Parameters.Add("prompt", "select_account");

        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetExternalLoginCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        ReturnUrl = returnUrl;

        if (remoteError != null)
        {
            _logger.LogWarning("External login remote error: {Error}", remoteError);
            Error = $"Error from external provider: {remoteError}";
            return Page();
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("Failed to load external login information from Google");
            return RedirectToPage("./Login", new { error = "Unable to load external login information. Please try again." });
        }

        var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? info.Principal.FindFirst("email")?.Value
                    ?? info.Principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

        var name = info.Principal.FindFirst(ClaimTypes.Name)?.Value
                   ?? info.Principal.FindFirst("name")?.Value;

        _logger.LogInformation(
            "External login info received from {Provider} - Email: {Email}, Name: {Name}, ProviderKey: {ProviderKey}",
            info.LoginProvider, email, name, info.ProviderKey);

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogError("No email found in Google claims");
            return RedirectToPage("./Login", new { error = "Unable to get email from Google. Please ensure your Google account has a public email." });
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return await RedirectBasedOnRole(existingUser);
            }

            return LocalRedirect(returnUrl ?? "/");
        }

        if (signInResult.IsLockedOut)
        {
            return RedirectToPage("./Login", new { error = "User account locked out." });
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user: {Errors}", errors);
                return RedirectToPage("./Login", new { error = $"Failed to create account: {errors}" });
            }

            await _userManager.AddToRoleAsync(user, "Host");

            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to link external login: {Errors}", errors);
                return RedirectToPage("./Login", new { error = $"Failed to link external login: {errors}" });
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

            return await RedirectBasedOnRole(user);
        }
        else
        {
            var linkResult = await _userManager.AddLoginAsync(user, info);
            if (!linkResult.Succeeded)
            {
                var errors = string.Join(", ", linkResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to link existing external login: {Errors}", errors);
                return RedirectToPage("./Login", new { error = $"Failed to link external login: {errors}" });
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return await RedirectBasedOnRole(user);
        }
    }

    private async Task<IActionResult> RedirectBasedOnRole(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var jwtToken = await _tokenService.GenerateJwtTokenAsync(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

        HttpContext.Session.SetString("JwtToken", jwtToken);
        HttpContext.Session.SetString("RefreshToken", refreshToken);
        HttpContext.Session.SetString("UserId", user.Id);
        HttpContext.Session.SetString("UserEmail", user.Email!);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(24)
        };

        Response.Cookies.Append("AuthToken", jwtToken, cookieOptions);
        Response.Cookies.Append("RefreshToken", refreshToken, cookieOptions);

        _logger.LogInformation("User {Email} has roles: {Roles}. Tokens saved to database.", user.Email, string.Join(", ", roles));

        if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
        {
            var adminUrl = _configuration["AdminUrl"] ?? "https://localhost:5220";
            var simpleToken = GenerateSimpleToken(user.Email!, roles);

            _logger.LogInformation("Redirecting admin {Email} to: {AdminUrl}", user.Email, adminUrl);
            return Redirect($"{adminUrl}/Auth/AdminLogin?token={Uri.EscapeDataString(simpleToken)}");
        }

        if (roles.Contains("Customer") || roles.Contains("Tenant") || roles.Contains("User"))
        {
            await ClearHostAuthenticationAsync();
            _logger.LogInformation("Customer/Tenant account detected in Host portal. Redirecting to Customer login page only.");
            return Redirect(CustomerPortalUrl);
        }

        if (roles.Contains("Host"))
        {
            _logger.LogInformation("Redirecting host {Email} to Host profile page", user.Email);
            return Redirect("/Host/Profile");
        }

        _logger.LogWarning("No role match for user {Email}, roles: {Roles}. Redirecting to Index", user.Email, string.Join(", ", roles));
        return RedirectToPage("/Index");
    }

    private async Task ClearHostAuthenticationAsync()
    {
        try
        {
            await _signInManager.SignOutAsync();
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
        catch
        {
        }

        HttpContext.Session.Clear();

        Response.Cookies.Delete("AuthToken");
        Response.Cookies.Delete("RefreshToken");
        Response.Cookies.Delete(".AspNetCore.Identity.Application");
        Response.Cookies.Delete(".AspNetCore.Session");
    }

    private string GenerateSimpleToken(string email, IList<string> roles)
    {
        try
        {
            var tokenData = $"{email}|{string.Join(",", roles)}|{DateTime.UtcNow.AddMinutes(30):yyyy-MM-dd HH:mm:ss}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tokenData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token for {Email}", email);
            return string.Empty;
        }
    }
}