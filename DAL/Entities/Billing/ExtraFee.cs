using DAL.Entities.Common;

namespace DAL.Entities.Billing;

public class ExtraFee : AuditableEntity<int>
{
    public string Name { get; set; } = null!;
    public decimal DefaultAmount { get; set; }
    public bool IsActive { get; set; } = true;
}
