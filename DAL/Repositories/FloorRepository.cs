using DAL.Data;
using DAL.Entities.Property;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class FloorRepository : IFloorRepository
{
    private readonly AppDbContext _db;
    public FloorRepository(AppDbContext db) => _db = db;

    public Task<List<Floor>> GetByBlockIdAsync(int blockId, CancellationToken ct = default)
        => _db.Floors
              .Where(f => f.BlockId == blockId)
              .AsNoTracking()
              .ToListAsync(ct);

    public Task<Floor?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Floors
              .Include(f => f.Rooms)
              .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task AddAsync(Floor floor, CancellationToken ct = default)
    {
        _db.Floors.Add(floor);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Floor floor, CancellationToken ct = default)
    {
        _db.Floors.Update(floor);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Floor floor, CancellationToken ct = default)
    {
        _db.Floors.Remove(floor);
        await _db.SaveChangesAsync(ct);
    }
}