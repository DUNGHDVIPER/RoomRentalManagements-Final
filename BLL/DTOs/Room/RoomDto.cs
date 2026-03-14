using DAL.Entities.Common;

namespace BLL.DTOs.Property;

public class RoomDto
{
    public int RoomId { get; set; }
    public int FloorId { get; set; }

    public string RoomCode { get; set; } = null!;
    public string? RoomName { get; set; }

    public decimal? AreaM2 { get; set; }
    public int MaxOccupants { get; set; }
    public RoomStatus Status { get; set; }

    public decimal CurrentBasePrice { get; set; }
    public string? Description { get; set; }

    public int? FloorNumber { get; set; }
    public string? BlockName { get; set; }

    // ===== ADD THIS =====
    public List<int> AmenityIds { get; set; } = new();
}