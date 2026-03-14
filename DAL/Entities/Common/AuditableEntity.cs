namespace DAL.Entities.Common;

public abstract class AuditableEntity<T> : EntityBase<T>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
