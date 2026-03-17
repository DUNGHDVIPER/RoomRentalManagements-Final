using BLL.DTOs.Property;
using BLL.Services.Interfaces;
using DAL.Entities.Common;
using DAL.Entities.Property;
using DAL.Repositories.Interfaces;

namespace BLL.Services;

public class BlockService : IBlockService
{
    private readonly IBlockRepository _repo;

    public BlockService(IBlockRepository repo)
    {
        _repo = repo;
    }

    // ================= GET ALL =================
    public async Task<IReadOnlyList<BlockDto>> GetAllAsync(CancellationToken ct = default)
    {
        var blocks = await _repo.GetAllAsync(ct);

        return blocks.Select(x => new BlockDto
        {
            BlockId = x.Id,
            BlockName = x.BlockName,
            Address = x.Address,
            Note = x.Note,
            Status = x.Status,

            TotalFloors = x.Floors?.Count ?? 0,
            TotalRooms = x.Floors?
                .SelectMany(f => f.Rooms ?? Enumerable.Empty<Room>())
                .Count() ?? 0
        }).ToList();
    }

    // ================= GET BY ID =================
    public async Task<BlockDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var block = await _repo.GetByIdAsync(id, ct);

        if (block == null)
            return null;

        return new BlockDto
        {
            BlockId = block.Id,
            BlockName = block.BlockName,
            Address = block.Address,
            Note = block.Note,
            Status = block.Status,

            TotalFloors = block.Floors?.Count ?? 0,
            TotalRooms = block.Floors?
                .SelectMany(f => f.Rooms ?? Enumerable.Empty<Room>())
                .Count() ?? 0
        };
    }

    // ================= CREATE =================
    public async Task<int> CreateAsync(BlockDto dto, CancellationToken ct = default)
    {
        var name = dto.BlockName!.Trim();

        var exists = await _repo.ExistsByNameAsync(name, null, ct);

        if (exists)
            throw new InvalidOperationException("Block name already exists");

        var block = new Block
        {
            BlockName = name,
            Address = dto.Address,
            Note = dto.Note,
            Status = "Active"
        };

        await _repo.AddAsync(block, ct);

        return block.Id;
    }

    // ================= UPDATE =================
    public async Task UpdateAsync(int id, BlockDto dto, CancellationToken ct = default)
    {
        var block = await _repo.GetByIdAsync(id, ct);

        if (block == null)
            throw new KeyNotFoundException($"Block {id} not found");

        var name = dto.BlockName!.Trim();

        var exists = await _repo.ExistsByNameAsync(name, id, ct);

        if (exists)
            throw new InvalidOperationException("Block name already exists");

        block.BlockName = name;
        block.Address = dto.Address;
        block.Note = dto.Note;

        await _repo.UpdateAsync(block, ct);
    }

    // ================= CLOSE BLOCK =================
    public async Task CloseAsync(int id, CancellationToken ct = default)
    {
        var block = await _repo.GetByIdAsync(id, ct);

        if (block == null)
            throw new KeyNotFoundException($"Block {id} not found");

        if (block.Status == "Closed")
            return;

        var hasOccupiedRooms = block.Floors?
            .SelectMany(f => f.Rooms ?? Enumerable.Empty<Room>())
            .Any(r => r.Status == RoomStatus.Occupied) ?? false;

        if (hasOccupiedRooms)
            throw new InvalidOperationException("Cannot close block with occupied rooms");

        block.Status = "Closed";

        await _repo.UpdateAsync(block, ct);
    }

    // ================= REOPEN BLOCK =================
    public async Task ReopenAsync(int id, CancellationToken ct = default)
    {
        var block = await _repo.GetByIdAsync(id, ct);

        if (block == null)
            throw new KeyNotFoundException($"Block {id} not found");

        if (block.Status == "Active")
            return;

        block.Status = "Active";

        await _repo.UpdateAsync(block, ct);
    }
}