using DAL.Entities.Common;
using DAL.Entities.Property;

namespace DAL.Entities.Tenanting;

public class StayHistory : AuditableEntity<int>
{
    public int RoomId { get; set; }
    public int TenantId { get; set; }

    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public string? Note { get; set; }

    public Room Room { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
