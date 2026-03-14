using System.ComponentModel.DataAnnotations;
using DAL.Entities.Common;
using DAL.Entities.Property;
using DAL.Entities.Tenanting;

namespace DAL.Entities.Maintenance;

public class Ticket : AuditableEntity<int>
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required, MaxLength(255)]
    public string Description { get; set; } = null!;

    [Required, MaxLength(50)]
    public TicketCategory Category { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public int RoomId { get; set; }
    public int? TenantId { get; set; }

    public Room? Room { get; set; }
    public Tenant? Tenant { get; set; }
}   