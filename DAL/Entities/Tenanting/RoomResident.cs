using System.ComponentModel.DataAnnotations;
using DAL.Entities.Property;

namespace DAL.Entities.Tenanting;

public class RoomResident
{
    [Key]
    public long ResidentId { get; set; }

    public int RoomId { get; set; }
    public int TenantId { get; set; }

    public DateTime CheckInDate { get; set; } = DateTime.UtcNow;
    public DateTime? CheckOutDate { get; set; }

    public bool IsActive { get; set; } = true;

    public Room Room { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}