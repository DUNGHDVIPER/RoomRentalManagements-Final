using DAL.Data;
using DAL.Entities.Property;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class BlockRepository : IBlockRepository
{
    private readonly AppDbContext _db;
    public BlockRepository(AppDbContext db) => _db = db;

public Task<List<Block>> GetAllAsync(CancellationToken ct = default)
    => _db.Blocks
        .Include(b => b.Floors)
        .ThenInclude(f => f.Rooms)
        .AsNoTracking()
        .ToListAsync(ct);

    public Task<Block?> GetByIdAsync(int id, CancellationToken ct = default)
      => _db.Blocks
          .Include(b => b.Floors)
          .ThenInclude(f => f.Rooms)
          .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task AddAsync(Block block, CancellationToken ct = default)
    {
        _db.Blocks.Add(block);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Block block, CancellationToken ct = default)
    {
        _db.Blocks.Update(block);
        await _db.SaveChangesAsync(ct);
    }

    public async Task CloseAsync(Block block, CancellationToken ct = default)
    {
        _db.Blocks.Remove(block);
        await _db.SaveChangesAsync(ct);
    }
}