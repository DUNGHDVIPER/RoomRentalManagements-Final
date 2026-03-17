namespace BLL.DTOs.Room;

public class UpdateRoomDto
{
    public string RoomNo { get; set; } = null!;
    public string? Name { get; set; }
    public decimal BasePrice { get; set; }
    public int Status { get; set; }
    public int[] AmenityIds { get; set; } = Array.Empty<int>();
}
