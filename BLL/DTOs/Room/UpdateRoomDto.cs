using DAL.Entities.Common;

namespace BLL.DTOs.Room;

public class UpdateRoomDto
{
    public int RoomId { get; set; }

    public string RoomCode { get; set; } = null!;

    public string? RoomName { get; set; }

    public decimal? AreaM2 { get; set; }

    public int MaxOccupants { get; set; }

    public RoomStatus Status { get; set; }

    public decimal CurrentBasePrice { get; set; }

    public string? Description { get; set; }

    public int[] AmenityIds { get; set; } = Array.Empty<int>();
}