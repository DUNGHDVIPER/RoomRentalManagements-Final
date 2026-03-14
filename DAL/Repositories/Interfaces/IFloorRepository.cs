using DAL.Entities.Property;

public interface IFloorRepository
{
    Task<List<Floor>> GetByBlockIdAsync(int blockId, CancellationToken ct = default);
    Task<Floor?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Floor floor, CancellationToken ct = default);
    Task UpdateAsync(Floor floor, CancellationToken ct = default);
    Task DeleteAsync(Floor floor, CancellationToken ct = default);
}