using DAL.Entities.Motel;
using DAL.Entities.Property;

namespace DAL.Repositories.Abstractions;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Room entity, CancellationToken ct = default);
    Task UpdateAsync(Room entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Room?> GetByIdAsync(int id, CancellationToken ct = default);
}
