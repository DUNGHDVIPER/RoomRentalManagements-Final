using DAL.Entities.Property;
using DAL.Entities.Common;
using DAL.Entities.Contracts;

namespace DAL.Entities.Property;

public class Room : AuditableEntity<int>
{
    public int FloorId { get; set; }
    public string RoomNo { get; set; } = null!;   // ví dụ: A101
    public string? Name { get; set; }            // tên hiển thị
    public RoomStatus Status { get; set; } = RoomStatus.Available;

    public decimal BasePrice { get; set; }

    public Floor Floor { get; set; } = null!;
    public ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();
    public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
    public ICollection<RoomPriceHistory> RoomPriceHistories { get; set; } = new List<RoomPriceHistory>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
