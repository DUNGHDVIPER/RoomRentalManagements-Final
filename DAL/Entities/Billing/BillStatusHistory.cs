using DAL.Entities.Common;

namespace DAL.Entities.Billing;

public class BillStatusHistory : AuditableEntity<int>
{
    public int BillId { get; set; }
    public int OldStatus { get; set; }
    public int NewStatus { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Bill Bill { get; set; } = null!;
}