using DAL.Entities.Common;

namespace DAL.Entities.Property;

public class Room
{
    // ===== PRIMARY KEY =====
    public int RoomId { get; set; }

    // ===== FOREIGN KEY =====
    public int FloorId { get; set; }

    // ===== BASIC INFO =====
    public string RoomCode { get; set; } = null!;
    public string? RoomName { get; set; }

    public decimal? AreaM2 { get; set; }
    public int MaxOccupants { get; set; }

    // ENUM (EF sẽ convert sang string)
    public RoomStatus Status { get; set; } = RoomStatus.Available;

    public decimal CurrentBasePrice { get; set; }
    public string? Description { get; set; }

    // ===== AUDIT =====
    public DateTime CreatedAt { get; set; }

    // ===== NAVIGATION =====
    public Floor Floor { get; set; } = null!;
    public ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();
    public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
    public ICollection<RoomPricingHistory> RoomPricingHistories { get; set; } = new List<RoomPricingHistory>();
}