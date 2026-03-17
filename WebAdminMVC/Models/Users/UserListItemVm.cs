using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Users;

/// <summary>
/// View model for displaying user information in list/table format
/// </summary>
public class UserListItemVm
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    [Required]
    [Display(Name = "User ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    [Required]
    [Display(Name = "Email Address")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;


    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;


    [Required]
    [Display(Name = "Status")]
    public string Status { get; set; } = string.Empty;

    [Display(Name = "Created Date")]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }

   
    public bool IsLocked => Status.Equals("Locked", StringComparison.OrdinalIgnoreCase);

  
    public bool IsActive => Status.Equals("Active", StringComparison.OrdinalIgnoreCase);

   
    public string Initials => string.IsNullOrEmpty(Email) ? "?" : Email.Substring(0, 1).ToUpper();

   
    public string StatusCssClass => IsActive ? "chip success" : "chip danger";

   
    public string StatusDisplayText => IsLocked ? "Locked" : "Active";

    public UserListItemVm()
    {
        CreatedAt = DateTime.UtcNow;
    }

 
    public UserListItemVm(string id, string email, string role, bool isLocked, DateTime? createdAt = null)
    {
        Id = id ?? string.Empty;
        Email = email ?? string.Empty;
        Role = role ?? string.Empty;
        Status = isLocked ? "Locked" : "Active";
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}