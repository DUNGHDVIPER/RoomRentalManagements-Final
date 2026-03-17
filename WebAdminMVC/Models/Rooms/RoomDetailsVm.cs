namespace WebAdmin.MVC.Models.Rooms;

public class RoomDetailsVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Block { get; set; } = null!;
    public string Floor { get; set; } = null!;
    public decimal Price { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow.AddDays(-14);
}
