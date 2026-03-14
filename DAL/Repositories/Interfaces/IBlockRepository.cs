using DAL.Entities.Property;

public interface IBlockRepository
{
    Task<List<Block>> GetAllAsync(CancellationToken ct = default);
    Task<Block?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Block block, CancellationToken ct = default);
    Task UpdateAsync(Block block, CancellationToken ct = default);
    Task CloseAsync(Block block, CancellationToken ct = default);
}