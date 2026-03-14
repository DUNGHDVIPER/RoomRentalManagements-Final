using DAL.Entities.Common;
using DAL.Entities.Property;

namespace DAL.Entities.Billing;

public class UtilityReading : AuditableEntity<int>
{
    public int RoomId { get; set; }

    // YYYYMM dạng int (202602…)
    public int Period { get; set; }

    public decimal ElectricKwh { get; set; }
    public decimal WaterM3 { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Room Room { get; set; } = null!;
}
