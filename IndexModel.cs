using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace WebHostRazor.Pages.Host.Profile;

public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public IndexModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public ProfileUpdateModel ProfileForm { get; set; } = new();

    [BindProperty]
    public PasswordChangeModel PasswordForm { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            ProfileForm.Email = user.Email ?? string.Empty;
            ProfileForm.PhoneNumber = user.PhoneNumber ?? string.Empty;
            ProfileForm.FullName = GetDisplayName(user);
        }
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (!ModelState.IsValid)
        {
            await LoadUserDataAsync();
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            ErrorMessage = "User not found.";
            return Page();
        }

        // Update user information
        user.Email = ProfileForm.Email;
        user.UserName = ProfileForm.Email; // Keep username same as email
        user.PhoneNumber = ProfileForm.PhoneNumber;

        // For demo purposes, we'll store full name in a custom claim
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
            await LoadUserDataAsync();
            return Page();
        }

        // Update display name claim
        var existingNameClaim = await _userManager.GetClaimsAsync(user);
        var nameClaim = existingNameClaim.FirstOrDefault(c => c.Type == "FullName");
        
        if (nameClaim != null)
        {
            await _userManager.RemoveClaimAsync(user, nameClaim);
        }
        
        if (!string.IsNullOrWhiteSpace(ProfileForm.FullName))
        {
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("FullName", ProfileForm.FullName));
        }

        SuccessMessage = "Profile updated successfully!";
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        // Clear profile form validation errors
        ModelState.Remove("ProfileForm.Email");
        ModelState.Remove("ProfileForm.FullName");

        if (!ModelState.IsValid)
        {
            await LoadUserDataAsync();
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            ErrorMessage = "User not found.";
            return Page();
        }

        var changePasswordResult = await _userManager.ChangePasswordAsync(
            user, PasswordForm.CurrentPassword, PasswordForm.NewPassword);

        if (!changePasswordResult.Succeeded)
        {
            ErrorMessage = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
            await LoadUserDataAsync();
            return Page();
        }

        // Refresh the sign-in to update security stamp
        await _signInManager.RefreshSignInAsync(user);

        SuccessMessage = "Password changed successfully!";
        
        // Clear password form
        PasswordForm = new PasswordChangeModel();
        
        await LoadUserDataAsync();
        return Page();
    }

    private async Task LoadUserDataAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            ProfileForm.Email = user.Email ?? string.Empty;
            ProfileForm.PhoneNumber = user.PhoneNumber ?? string.Empty;
            ProfileForm.FullName = GetDisplayName(user);
        }
    }

    private string GetDisplayName(IdentityUser user)
    {
        // For simplicity, extract name from email
        return user.Email?.Split('@')[0] ?? "User";
    }
}

public class ProfileUpdateModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
    public string Email { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Full name must not exceed 50 characters")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number")]
    [RegularExpression(@"^(\+84|0)[0-9]{9,10}$", ErrorMessage = "Please enter a valid Vietnamese phone number")]
    public string? PhoneNumber { get; set; }
}

public class PasswordChangeModel
{
    [Required(ErrorMessage = "Current password is required")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, lowercase letter, number, and special character")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}