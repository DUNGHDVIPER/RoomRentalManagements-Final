using DAL.Entities.Common;

namespace DAL.Entities.Billing;

public class Payment : AuditableEntity<int>
{
    public int BillId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash"; // Cash/Banking/Momo/…
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime? PaidAt { get; set; }
    public string? TransactionRef { get; set; }

    public Bill Bill { get; set; } = null!;
}
