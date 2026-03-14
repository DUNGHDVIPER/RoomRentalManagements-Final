using DAL.Data;
using DAL.Entities.Property;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Implementations;

public sealed class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _db;

    public RoomRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default)
        => await _db.Rooms
            .AsNoTracking()
            .OrderBy(r => r.RoomCode)
            .ToListAsync(ct);

    public Task<Room?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoomId == id, ct);

    public async Task AddAsync(Room entity, CancellationToken ct = default)
    {
        _db.Rooms.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Room entity, CancellationToken ct = default)
    {
        _db.Rooms.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FindAsync(new object[] { id }, ct);
        if (room == null) return;

        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync(ct);
    }
}