using DAL.Entities.Common;

namespace DAL.Entities.Property;

public class Block : AuditableEntity<int>
{
    public string BlockName { get; set; } = null!;
    public string? Address { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "Active";
    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
}