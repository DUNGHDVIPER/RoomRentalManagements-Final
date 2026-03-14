namespace BLL.DTOs.Tenant;

public class TenantDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public DateTime? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? IdentityUserId { get; set; }
    public bool IsBlacklisted { get; set; }

    public string? CCCD { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = "Active";

    public DateTime? CheckInDate { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}