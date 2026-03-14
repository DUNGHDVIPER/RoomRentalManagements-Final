namespace BLL.DTOs.Tenant;

public class TenantIdDocDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string DocType { get; set; } = "CCCD";
    public string DocNumber { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
}
