using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Users;

public class UserEditVm
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = "Host";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}