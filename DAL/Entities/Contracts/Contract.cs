using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DAL.Entities.Billing;
using DAL.Entities.Common;
using DAL.Entities.Motel;     // chỉ để dùng User (auth/audit)
using DAL.Entities.Property;  // Room
using DAL.Entities.Tenanting; // Tenant

namespace DAL.Entities.Contracts;

public class Contract : AuditableEntity<int>
{
    [Key]
    public int ContractId { get; set; }

    public int RoomId { get; set; }
    public int TenantId { get; set; }

    [MaxLength(50)]
    public string ContractCode { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseRent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DepositAmount { get; set; }

    [MaxLength(20)]
    public string PaymentCycle { get; set; } = "Monthly";

    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    [MaxLength(500)]
    public string? Note { get; set; }

    public int? CreatedByUserId { get; set; }

    // ✅ Navigation đúng namespace mới
    public Room Room { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public User? CreatedByUser { get; set; }

    public ICollection<ContractAttachment> Attachments { get; set; } = new List<ContractAttachment>();
    public ICollection<ContractVersion> Versions { get; set; } = new List<ContractVersion>();
    public ICollection<Deposit> Deposits { get; set; } = new List<Deposit>();

    [MaxLength(20)]
    public string DepositStatus { get; set; } = "Unpaid";

    public DateTime? DepositPaidAt { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DepositPaidAmount { get; set; }

    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
}