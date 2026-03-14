namespace BLL.DTOs.Room.Public;

public class RoomPublicDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public string District { get; set; } = "";

    public decimal Price { get; set; }
    public double Area { get; set; }

    public string PostedAgo { get; set; } = "vừa đăng";
    public List<string> Badges { get; set; } = new();

    public List<RoomImagePublicDto> Images { get; set; } = new();
    public List<AmenityPublicDto> Amenities { get; set; } = new();
}
