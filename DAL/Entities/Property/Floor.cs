using DAL.Entities.Common;

namespace DAL.Entities.Property;

public class Floor : AuditableEntity<int>
{
    public int BlockId { get; set; }
    public string FloorName { get; set; } = null!;

    public Block Block { get; set; } = null!;
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}