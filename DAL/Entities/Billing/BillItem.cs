using DAL.Entities.Common;

namespace DAL.Entities.Billing;

public class BillItem : AuditableEntity<int>
{
    public int BillId { get; set; }

    public string Name { get; set; } = null!;
    public decimal Amount { get; set; }

    // Optional link to ExtraFee catalog
    public int? ExtraFeeId { get; set; }
    public ExtraFee? ExtraFee { get; set; }

    public Bill Bill { get; set; } = null!;
}
