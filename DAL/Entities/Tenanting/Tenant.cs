using DAL.Entities.Tenanting;
using DAL.Entities.Common;
using DAL.Entities.Contracts;
using Microsoft.AspNetCore.Identity;

namespace DAL.Entities.Tenanting;

public class Tenant : AuditableEntity<int>
{
    public string FullName { get; set; } = null!;
    public DateTime? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string? CCCD { get; set; }
    public string? Gender { get; set; }

    public string Status { get; set; } = "Active";
    public string? Address { get; set; }

    // Optional link to Identity User
    //public string? UserId { get; set; }
    public string UserId { get; set; } = null!;

    public IdentityUser User { get; set; } = null!;

    public ICollection<TenantIdDoc> IdDocs { get; set; } = new List<TenantIdDoc>();
    public ICollection<StayHistory> StayHistories { get; set; } = new List<StayHistory>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
