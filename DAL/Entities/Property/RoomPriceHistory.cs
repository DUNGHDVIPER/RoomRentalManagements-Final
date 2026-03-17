using DAL.Entities.Common;

namespace DAL.Entities.Property;

public class RoomPriceHistory : AuditableEntity<int>
{
    public int RoomId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal Price { get; set; }

    public Room Room { get; set; } = null!;
}
