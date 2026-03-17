using DAL.Entities.Common;

namespace DAL.Entities.Property;

public class RoomImage : AuditableEntity<int>
{
    public int RoomId { get; set; }
    public string Url { get; set; } = null!;
    public bool IsCover { get; set; }
    public int SortOrder { get; set; }

    public Room Room { get; set; } = null!;
}
