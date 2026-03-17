namespace DAL.Entities.Property;

public class RoomAmenity
{
    public int RoomId { get; set; }
    public int AmenityId { get; set; }

    public Room Room { get; set; } = null!;
    public Amenity Amenity { get; set; } = null!;
}
