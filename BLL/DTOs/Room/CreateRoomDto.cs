using DAL.Entities.Common;

public class CreateRoomDto
{
    public int FloorId { get; set; }

    public string RoomCode { get; set; }

    public string? RoomName { get; set; }

    public decimal? AreaM2 { get; set; }

    public int MaxOccupants { get; set; }

    public RoomStatus Status { get; set; }

    public decimal CurrentBasePrice { get; set; }

    public string? Description { get; set; }

    public int[] AmenityIds { get; set; }
}