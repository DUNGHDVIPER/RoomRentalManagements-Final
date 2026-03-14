using DAL.Entities.Common;
using System.ComponentModel.DataAnnotations;

namespace DAL.Entities.Property;

public class Amenity : AuditableEntity<int>
{
    [Required]
    [MaxLength(100)]
    public string AmenityName { get; set; } = null!;

    public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
}