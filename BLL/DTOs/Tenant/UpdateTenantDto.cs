namespace BLL.DTOs.Tenant;

public class UpdateTenantDto
{
    public string FullName { get; set; } = null!;
    public DateTime? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? IdentityUserId { get; set; }
}
