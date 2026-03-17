using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Models.Rooms;

public class RoomCreateVms
{
    [Required] public string Name { get; set; } = null!;
    [Required] public string Block { get; set; } = "A";
    [Required] public string Floor { get; set; } = "1";
    [Range(0, 999999999)] public decimal Price { get; set; }
    [Required] public string Status { get; set; } = "Available";
}
