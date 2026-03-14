using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace WebHostRazor.Pages.Auth;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(UserManager<IdentityUser> userManager, ILogger<ResetPasswordModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public ResetPasswordRequest ResetPasswordRequest { get; set; } = new();

    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
    public bool ResetCompleted { get; set; }

    public async Task<IActionResult> OnGetAsync(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            Message = "Invalid reset password link.";
            IsSuccess = false;
            return Page();
        }

        // Kiểm tra user có tồn tại không
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Reset password attempted for non-existent user: {Email}", email);
            Message = "Invalid reset password link.";
            IsSuccess = false;
            return Page();
        }

        // ✅ KIỂM TRA TOKEN CÒN HỢP LỆ KHÔNG (quan trọng nhất)
        var isValidToken = await _userManager.VerifyUserTokenAsync(
            user,
            _userManager.Options.Tokens.PasswordResetTokenProvider,
            "ResetPassword",
            token);

        if (!isValidToken)
        {
            _logger.LogWarning("Invalid or expired reset password token for user: {Email}", email);
            Message = "This reset password link has already been used or has expired. Please request a new password reset.";
            IsSuccess = false;
            return Page();
        }

        // Token hợp lệ, cho phép reset
        ResetPasswordRequest.Email = email;
        ResetPasswordRequest.Token = token;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(ResetPasswordRequest.Email);
        if (user == null)
        {
            Message = "Invalid reset password request.";
            IsSuccess = false;
            return Page();
        }

        // ✅ KIỂM TRA LẠI TOKEN LẦN NỮA TRƯỚC KHI RESET
        var isValidToken = await _userManager.VerifyUserTokenAsync(
            user,
            _userManager.Options.Tokens.PasswordResetTokenProvider,
            "ResetPassword",
            ResetPasswordRequest.Token);

        if (!isValidToken)
        {
            _logger.LogWarning("Attempt to use invalid/expired token for password reset: {Email}", ResetPasswordRequest.Email);
            Message = "This reset password link has already been used or has expired. Please request a new password reset.";
            IsSuccess = false;
            return Page();
        }

        // Reset password
        var result = await _userManager.ResetPasswordAsync(user, ResetPasswordRequest.Token, ResetPasswordRequest.NewPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset successful for user: {Email}", ResetPasswordRequest.Email);

            // ✅ TOKEN TỰ ĐỘNG HẾT HẠN SAU KHI DÙNG 1 LẦN
            Message = "Your password has been reset successfully!";
            IsSuccess = true;
            ResetCompleted = true;

            // Optional: Invalidate all other tokens for security
            await _userManager.UpdateSecurityStampAsync(user);
        }
        else
        {
            _logger.LogWarning("Password reset failed for user: {Email}. Errors: {Errors}",
                ResetPasswordRequest.Email,
                string.Join(", ", result.Errors.Select(e => e.Description)));

            Message = string.Join(", ", result.Errors.Select(e => e.Description));
            IsSuccess = false;
        }

        return Page();
    }
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}