using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace WebHostRazor.Pages.Host.Profile;

[Authorize(Roles = "Host")]
public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<IndexModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public ProfileUpdateModel ProfileForm { get; set; } = new();

    [BindProperty]
    public PasswordChangeModel PasswordForm { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            await LoadUserDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profile data for user {UserId}", User.Identity?.Name);
            ErrorMessage = "Error loading profile data. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        try
        {
            // Clear previous messages
            ModelState.Remove(nameof(SuccessMessage));
            ModelState.Remove(nameof(ErrorMessage));

            // Only validate profile form, ignore password form
            ValidateProfileForm();

            if (!ModelState.IsValid)
            {
                await LoadUserDataAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found for profile update: {UserId}", User.Identity?.Name);
                ErrorMessage = "User not found.";
                return Page();
            }

            // Check if email is already taken by another user
            var existingUser = await _userManager.FindByEmailAsync(ProfileForm.Email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ErrorMessage = "Email address is already in use.";
                await LoadUserDataAsync();
                return Page();
            }

            // Update user information
            var updateResult = await UpdateUserInfoAsync(user);
            if (!updateResult.success)
            {
                ErrorMessage = updateResult.error;
                await LoadUserDataAsync();
                return Page();
            }

            SuccessMessage = "Profile updated successfully!";
            _logger.LogInformation("Profile updated successfully for user {UserId}", user.Id);

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", User.Identity?.Name);
            ErrorMessage = "An error occurred while updating your profile. Please try again.";
            await LoadUserDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        try
        {
            // Clear previous messages
            ModelState.Remove(nameof(SuccessMessage));
            ModelState.Remove(nameof(ErrorMessage));

            // Only validate password form, ignore profile form
            ValidatePasswordForm();

            if (!ModelState.IsValid)
            {
                await LoadUserDataAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found for password change: {UserId}", User.Identity?.Name);
                ErrorMessage = "User not found.";
                return Page();
            }

            // Verify current password first
            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, PasswordForm.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                ModelState.AddModelError($"{nameof(PasswordForm)}.{nameof(PasswordForm.CurrentPassword)}",
                    "Current password is incorrect.");
                await LoadUserDataAsync();
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user, PasswordForm.CurrentPassword, PasswordForm.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                var errors = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
                ErrorMessage = $"Password change failed: {errors}";
                _logger.LogWarning("Password change failed for user {UserId}: {Errors}", user.Id, errors);
                await LoadUserDataAsync();
                return Page();
            }

            // Refresh the sign-in to update security stamp
            await _signInManager.RefreshSignInAsync(user);

            SuccessMessage = "Password changed successfully!";
            _logger.LogInformation("Password changed successfully for user {UserId}", user.Id);

            // Clear password form after successful change
            PasswordForm = new PasswordChangeModel();

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", User.Identity?.Name);
            ErrorMessage = "An error occurred while changing your password. Please try again.";
            await LoadUserDataAsync();
            return Page();
        }
    }

    private async Task LoadUserDataAsync()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ProfileForm.Email = user.Email ?? string.Empty;
                ProfileForm.PhoneNumber = user.PhoneNumber ?? string.Empty;

                // Get full name from claims
                var fullNameClaim = User.FindFirst("FullName");
                if (fullNameClaim?.Value != null)
                {
                    ProfileForm.FullName = fullNameClaim.Value;
                }
                else
                {
                    // Fallback to generating display name from email
                    ProfileForm.FullName = GetDisplayNameFromEmail(user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user data for {UserId}", User.Identity?.Name);
            ProfileForm = new ProfileUpdateModel();
        }
    }

    private async Task<(bool success, string? error)> UpdateUserInfoAsync(IdentityUser user)
    {
        try
        {
            // Update basic user information
            user.Email = ProfileForm.Email;
            user.UserName = ProfileForm.Email; // Keep username in sync with email
            user.PhoneNumber = string.IsNullOrWhiteSpace(ProfileForm.PhoneNumber) ? null : ProfileForm.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                _logger.LogWarning("User update failed for {UserId}: {Errors}", user.Id, errors);
                return (false, errors);
            }

            // Update full name claim if provided
            if (!string.IsNullOrWhiteSpace(ProfileForm.FullName))
            {
                await UpdateFullNameClaimAsync(user, ProfileForm.FullName.Trim());
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user info for {UserId}", user.Id);
            return (false, "An unexpected error occurred while updating your information.");
        }
    }

    private async Task UpdateFullNameClaimAsync(IdentityUser user, string fullName)
    {
        try
        {
            var existingClaims = await _userManager.GetClaimsAsync(user);
            var nameClaim = existingClaims.FirstOrDefault(c => c.Type == "FullName");

            if (nameClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, nameClaim);
            }

            await _userManager.AddClaimAsync(user, new Claim("FullName", fullName));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update FullName claim for user {UserId}", user.Id);
            // Don't throw - claim update failure shouldn't break profile update
        }
    }

    private void ValidateProfileForm()
    {
        // Remove password form validation errors
        var keysToRemove = ModelState.Keys
            .Where(key => key.StartsWith($"{nameof(PasswordForm)}."))
            .ToList();

        foreach (var key in keysToRemove)
        {
            ModelState.Remove(key);
        }

        // Additional custom validation for profile
        if (string.IsNullOrWhiteSpace(ProfileForm.Email))
        {
            ModelState.AddModelError($"{nameof(ProfileForm)}.{nameof(ProfileForm.Email)}",
                "Email is required.");
        }

        if (!string.IsNullOrWhiteSpace(ProfileForm.FullName) && ProfileForm.FullName.Length > 100)
        {
            ModelState.AddModelError($"{nameof(ProfileForm)}.{nameof(ProfileForm.FullName)}",
                "Full name cannot exceed 100 characters.");
        }
    }

    private void ValidatePasswordForm()
    {
        // Remove profile form validation errors
        var keysToRemove = ModelState.Keys
            .Where(key => key.StartsWith($"{nameof(ProfileForm)}."))
            .ToList();

        foreach (var key in keysToRemove)
        {
            ModelState.Remove(key);
        }

        // Additional password validation
        if (string.IsNullOrWhiteSpace(PasswordForm.CurrentPassword))
        {
            ModelState.AddModelError($"{nameof(PasswordForm)}.{nameof(PasswordForm.CurrentPassword)}",
                "Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(PasswordForm.NewPassword))
        {
            ModelState.AddModelError($"{nameof(PasswordForm)}.{nameof(PasswordForm.NewPassword)}",
                "New password is required.");
        }

        if (PasswordForm.NewPassword != PasswordForm.ConfirmNewPassword)
        {
            ModelState.AddModelError($"{nameof(PasswordForm)}.{nameof(PasswordForm.ConfirmNewPassword)}",
                "Passwords do not match.");
        }
    }

    private static string GetDisplayNameFromEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[..atIndex] : email;
    }
}

public class ProfileUpdateModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Full name must not exceed 100 characters")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number")]
    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters")]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
}

public class PasswordChangeModel
{
    [Required(ErrorMessage = "Current password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}