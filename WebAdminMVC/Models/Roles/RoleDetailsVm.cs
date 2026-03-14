using System.ComponentModel.DataAnnotations;

namespace WebAdminMVC.Models.Roles;

public class RoleDetailsVm
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Users in Role")]
    public List<UserInRoleVm> Users { get; set; } = [];
}

public class UserInRoleVm
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}