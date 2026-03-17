using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Property;

public class FloorDto
{
    public int Id { get; set; }

    public int BlockId { get; set; }

    public string? BlockName { get; set; }   // chỉ hiển thị UI

    [Required(ErrorMessage = "Floor name is required")]
    [StringLength(100)]
    public string FloorName { get; set; } = string.Empty;

    public int TotalRooms { get; set; }
}