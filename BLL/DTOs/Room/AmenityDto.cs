using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Room;

public class AmenityDto
{
    public int AmenityId { get; set; }

    [Required(ErrorMessage = "Amenity name is required")]
    [StringLength(50, ErrorMessage = "Amenity name cannot exceed 50 characters")]
    public string AmenityName { get; set; } = null!;
}
