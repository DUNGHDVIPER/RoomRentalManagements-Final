using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Property;

public class BlockDto
{
    public int BlockId { get; set; }

    [Required(ErrorMessage = "Block name is required")]
    [StringLength(200)]
    public string BlockName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public string Status { get; set; } = "Active";

    public int TotalFloors { get; set; }

    public int TotalRooms { get; set; }
}