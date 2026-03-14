using System.ComponentModel.DataAnnotations;

namespace WebAdminMVC.Models.Roles;

public class RoleListItemVm
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "User Count")]
    public int UserCount { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; }
}