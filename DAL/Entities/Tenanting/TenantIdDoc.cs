using DAL.Entities.Common;

namespace DAL.Entities.Tenanting;

public class TenantIdDoc : AuditableEntity<int>
{
    public int TenantId { get; set; }
    public string DocType { get; set; } = "CCCD";
    public string DocNumber { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}