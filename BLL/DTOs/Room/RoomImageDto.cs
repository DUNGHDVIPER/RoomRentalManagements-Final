namespace BLL.DTOs.Room;

public class RoomImageDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string Url { get; set; } = null!;
    public bool IsCover { get; set; }
    public int SortOrder { get; set; }
}
