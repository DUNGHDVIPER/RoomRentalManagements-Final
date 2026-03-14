using DAL.Entities.Common;
using DAL.Entities.Contracts;

namespace DAL.Entities.Billing;

public class Bill : AuditableEntity<int>
{
    public int ContractId { get; set; }   // ✅ int -> long

    public int Period { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Draft;

    public decimal TotalAmount { get; set; }

    public Contract Contract { get; set; } = null!;
    public ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}