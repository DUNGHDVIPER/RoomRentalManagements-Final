using DAL.Entities.Common;

namespace DAL.Entities.Billing;

public class UtilityPrice : AuditableEntity<int>
{
    public DateTime EffectiveFrom { get; set; }
    public decimal ElectricPerKwh { get; set; }
    public decimal WaterPerM3 { get; set; }
}
