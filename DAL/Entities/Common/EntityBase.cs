namespace DAL.Entities.Common;

public abstract class EntityBase<T>
{
    public T Id { get; set; } = default!;
}
