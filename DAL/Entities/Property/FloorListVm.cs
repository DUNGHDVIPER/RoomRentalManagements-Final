namespace BLL.DTOs.Property;

public class FloorListVm
{
    public int Id { get; set; }
    public string FloorName { get; set; } = null!;
    public int TotalRooms { get; set; }
}