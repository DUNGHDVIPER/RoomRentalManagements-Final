using DAL.Entities.Common;
using System.ComponentModel.DataAnnotations;

namespace DAL.Entities.Property;

public class Amenity : AuditableEntity<int>
{
    // ❌ XÓA dòng object? AmenityId
    // public object? AmenityId { get; internal set; }

    // ✅ nếu DB cần cột AmenityId, ta map Id -> AmenityId ở Config
    [Required]
    public string AmenityName { get; set; } = null!;

    public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
}