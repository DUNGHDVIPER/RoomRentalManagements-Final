using DAL.Entities.Property;
using DAL.Entities.Common;

namespace DAL.Entities.Property;

public class Block : AuditableEntity<int>
{
    public string Name { get; set; } = null!;
    public string? Address { get; set; }

    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
}
