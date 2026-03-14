using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Auth;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
    public required string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    public required string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public required string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Full name can only contain letters and spaces")]
    public required string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    [RegularExpression(@"^(\+84|0)[0-9]{9,10}$", ErrorMessage = "Phone number must be Vietnamese format")]
    public string? PhoneNumber { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the Terms and Conditions")]
    public bool AcceptTerms { get; set; }
}