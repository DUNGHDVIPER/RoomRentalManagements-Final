using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace WebHostRazor.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;

    public RegisterModel(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public RegisterForm Form { get; set; } = new();

    public string? Error { get; set; }

    public void OnGet()
    {
        Error = null;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Error = null;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check if user exists
        var existingUser = await _userManager.FindByEmailAsync(Form.Email);
        if (existingUser != null)
        {
            Error = "User with this email already exists.";
            return Page();
        }

        // Create new user
        var user = new IdentityUser
        {
            UserName = Form.Email,
            Email = Form.Email,
            PhoneNumber = Form.PhoneNumber,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Form.Password);
        if (!result.Succeeded)
        {
            Error = string.Join(", ", result.Errors.Select(e => e.Description));
            return Page();
        }

        // Assign Host role
        try
        {
            await _userManager.AddToRoleAsync(user, "User");
        }
        catch (Exception)
        {
            Error = "Role assignment failed. Please contact administrator.";
            return Page();
        }

        // Set success message and redirect
        TempData["SuccessMessage"] = $"Registration successful! Welcome {Form.FullName}. You can now login.";
        TempData["UserEmail"] = Form.Email;

        return RedirectToPage("/Auth/Login");
    }
}

public class RegisterForm
{
    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email")]
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "You must accept the terms")]
    public bool AcceptTerms { get; set; }
}
