namespace WebAdmin.MVC.Models.Rooms;

public class RoomListItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Block { get; set; } = "A";
    public string Floor { get; set; } = "1";
    public decimal Price { get; set; }
    public string Status { get; set; } = "Available"; // Available / Occupied / Maintenance
}
