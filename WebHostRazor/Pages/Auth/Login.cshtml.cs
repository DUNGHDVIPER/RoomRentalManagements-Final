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

    // Properties for the view to bind to
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

    public async Task OnGetAsync(string? returnUrl = null, string? error = null)
    {
        ReturnUrl = returnUrl;
        Error = error; // Hiển thị error từ Google nếu có

        // Clear existing external authentication
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

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(Email);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    _logger.LogInformation("User {Email} logged in with roles: {Roles}", Email, string.Join(",", roles));

                    return await RedirectBasedOnRole(user);
                }

                // Default fallback redirect
                _logger.LogWarning("No user found for email {Email}, using default redirect", Email);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToPage("/Index"); // Default home page
            }

            Error = result.Error ?? "Invalid login attempt.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", Email);
            Error = "An error occurred during login. Please try again.";
            return Page();
        }
    }

    // GOOGLE LOGIN METHODS
    public async Task<IActionResult> OnPostExternalLoginAsync(string provider, string? returnUrl = null)
    {
        // Clear existing external authentication first
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        // Redirect về callback sau khi Google authentication thành công
        var redirectUrl = Url.Page("./Login", pageHandler: "ExternalLoginCallback", values: new { returnUrl });
        _logger.LogInformation("Starting external login with {Provider}, callback URL: {RedirectUrl}", provider, redirectUrl);

        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        properties.Parameters.Add("prompt", "select_account"); // Force account selection

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

            // TRỞ LẠI LOGIN PAGE VỚI ERROR MESSAGE
            return RedirectToPage("./Login", new { error = "Unable to load external login information. Please try again." });
        }

        // DEBUG: Log tất cả claims từ Google
        _logger.LogInformation("=== GOOGLE CLAIMS DEBUG ===");
        foreach (var claim in info.Principal.Claims)
        {
            _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
        }
        _logger.LogInformation("=== END GOOGLE CLAIMS ===");

        // THỬ CÁC CÁCH KHÁC NHAU ĐỂ LẤY EMAIL
        var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? info.Principal.FindFirst("email")?.Value
                    ?? info.Principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

        var name = info.Principal.FindFirst(ClaimTypes.Name)?.Value
                   ?? info.Principal.FindFirst("name")?.Value;
        _logger.LogInformation("External login info received from {Provider} - Email: {Email}, Name: {Name}, ProviderKey: {ProviderKey}",
                    info.LoginProvider, email, name, info.ProviderKey);

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogError("No email found in Google claims");
            return RedirectToPage("./Login", new { error = "Unable to get email from Google. Please ensure your Google account has a public email." });
        }

        // Thử sign in với external provider
        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);

            // Tìm user và generate tokens cho Google login
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // ✅ GENERATE VÀ SAVE TOKENS CHO GOOGLE LOGIN
                var roles = await _userManager.GetRolesAsync(user);
                var jwtToken = await _tokenService.GenerateJwtTokenAsync(user, roles);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // LƯU REFRESH TOKEN VÀO DATABASE
                await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);
                _logger.LogInformation("Tokens generated and saved for Google login user: {Email}", email);

                return await RedirectBasedOnRole(user);
            }

            return LocalRedirect(returnUrl ?? "/");
        }

        if (signInResult.IsLockedOut)
        {
            return RedirectToPage("./Login", new { error = "User account locked out." });
        }
        else
        {
            // Nếu user chưa có account, tạo account mới
            if (!string.IsNullOrEmpty(email))
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        // Thêm role Customer mặc định cho Google user
                        await _userManager.AddToRoleAsync(user, "Customer");

                        // Link external login
                        result = await _userManager.AddLoginAsync(user, info);
                        if (result.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                            // ✅ GENERATE VÀ SAVE TOKENS CHO USER MỚI TỪ GOOGLE
                            var roles = await _userManager.GetRolesAsync(user);
                            var jwtToken = await _tokenService.GenerateJwtTokenAsync(user, roles);
                            var refreshToken = _tokenService.GenerateRefreshToken();

                            // LƯU REFRESH TOKEN VÀO DATABASE
                            await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);
                            _logger.LogInformation("Tokens generated and saved for new Google user: {Email}", email);

                            return await RedirectBasedOnRole(user);
                        }
                    }

                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create user: {Errors}", errors);
                    return RedirectToPage("./Login", new { error = $"Failed to create account: {errors}" });
                }
                else
                {
                    // User tồn tại nhưng chưa link với Google
                    var result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        // ✅ GENERATE VÀ SAVE TOKENS CHO USER ĐÃ TỒN TẠI
                        var roles = await _userManager.GetRolesAsync(user);
                        var jwtToken = await _tokenService.GenerateJwtTokenAsync(user, roles);
                        var refreshToken = _tokenService.GenerateRefreshToken();

                        // LƯU REFRESH TOKEN VÀO DATABASE
                        await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);
                        _logger.LogInformation("Tokens generated and saved for existing user linked to Google: {Email}", email);

                        return await RedirectBasedOnRole(user);
                    }

                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to link external login: {Errors}", errors);
                }
            }

            return RedirectToPage("./Login", new { error = "Unable to process external login information." });
        }
    }

    private async Task<IActionResult> RedirectBasedOnRole(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        // GENERATE JWT TOKEN
        var jwtToken = await _tokenService.GenerateJwtTokenAsync(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // ✅ LƯU REFRESH TOKEN VÀO DATABASE
        await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

        // ✅ Điều chỉnh Secure cho development
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.IsHttps, // ✅ Chỉ Secure khi là HTTPS
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(24)
        };

        // Save tokens và user info vào cookies
        Response.Cookies.Append("AuthToken", jwtToken, cookieOptions);
        Response.Cookies.Append("RefreshToken", refreshToken, cookieOptions);
        Response.Cookies.Append("UserId", user.Id, cookieOptions);
        Response.Cookies.Append("UserEmail", user.Email!, cookieOptions);

        _logger.LogInformation("User {Email} has roles: {Roles}. Tokens saved to cookies and database.",
            user.Email, string.Join(", ", roles));

        // Redirect logic...
        if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
        {
            var adminUrl = _configuration["AdminUrl"] ?? "https://localhost:7282";
            _logger.LogInformation("Redirecting admin {Email} to: {AdminUrl}", user.Email, adminUrl);
            return Redirect($"{adminUrl}/Auth/AdminLogin?token={jwtToken}&userId={user.Id}");
        }

        if (roles.Contains("User") || roles.Contains("Customer") || roles.Contains("Tenant"))
        {
            var customerUrl = _configuration["CustomerUrl"] ?? "https://localhost:7292";
            _logger.LogInformation("Redirecting customer {Email} to: {CustomerUrl}", user.Email, customerUrl);
            return Redirect($"{customerUrl}/token-handler?token={jwtToken}&userId={user.Id}");
        }

        if (roles.Contains("Host"))
        {
            _logger.LogInformation("Redirecting host {Email} to Host dashboard", user.Email);
            return RedirectToPage("/Host/Profile/Index");
        }

        _logger.LogWarning("No role match for user {Email}, roles: {Roles}. Redirecting to Index",
            user.Email, string.Join(", ", roles));
        return RedirectToPage("/Index");
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