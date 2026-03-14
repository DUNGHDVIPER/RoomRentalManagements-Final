using DAL.Entities.Property;

namespace DAL.Repositories.Interfaces;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Room entity, CancellationToken ct = default);
    Task UpdateAsync(Room entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}