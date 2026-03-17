namespace BLL.DTOs.Room;

public class FloorDto
{
    public int Id { get; set; }
    public int BlockId { get; set; }
    public string Name { get; set; } = null!;
    public int Level { get; set; }
}
