using DAL.Data;
using DAL.Entities.Motel;
using DAL.Entities.Property;
using DAL.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using RoomEntity = DAL.Entities.Property.Room;

namespace DAL.Repositories.Implementations;

public sealed class RoomRepository : IRoomRepository

{
    private readonly AppDbContext _db;
    public RoomRepository(AppDbContext db) => _db = db;

    public Task<RoomEntity?> GetByIdAsync(int id, CancellationToken ct = default)
     => _db.Rooms
         .AsNoTracking()
         .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default)
    => (IReadOnlyList<Room>)await _db.Rooms
        .AsNoTracking()
        .OrderBy(r => r.RoomNo)   // ✅ RoomNo
        .ToListAsync(ct);

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Room entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Room entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    Task<Room?> IRoomRepository.GetByIdAsync(int id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
