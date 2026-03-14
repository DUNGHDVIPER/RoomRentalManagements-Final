namespace WebAdmin.MVC.Models.Users;

public class UserDetailsVm
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddDays(-7);
}
