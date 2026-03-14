using System.ComponentModel.DataAnnotations;

namespace WebAdminMVC.Models.Roles;

public class RoleCreateVm
{
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;
}