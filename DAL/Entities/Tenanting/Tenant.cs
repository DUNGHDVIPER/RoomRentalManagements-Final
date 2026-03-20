using DAL.Entities.Tenanting;
using DAL.Entities.Common;
using DAL.Entities.Contracts;
using Microsoft.AspNetCore.Identity;
using DAL.Entities.Property;

namespace DAL.Entities.Tenanting;

public class Tenant : AuditableEntity<int>
{
    public string FullName { get; set; } = null!;
    public DateTime? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string? CCCD { get; set; }
    public string? Gender { get; set; }

    //public string Status { get; set; } = "Active";
    public string? Address { get; set; }
    public string? BlacklistReason { get; set; }
    public DateTime? BlacklistedAt { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;


    // Optional link to Identity User
    //public string? UserId { get; set; }
    public string UserId { get; set; } = null!;

    // Link tới Room
    public int? RoomId { get; set; }
    public Room? Room { get; set; }

    public IdentityUser User { get; set; } = null!;

    public ICollection<TenantIdDoc> IdDocs { get; set; } = new List<TenantIdDoc>();
    public ICollection<StayHistory> StayHistories { get; set; } = new List<StayHistory>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
