using BLL.DTOs.Property;
using BLL.Services.Interfaces;
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
            TotalRooms = x.Floors?.SelectMany(f => f.Rooms).Count() ?? 0
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
            Status = block.Status
        };
    }

    // ================= CREATE =================
    public async Task<int> CreateAsync(BlockDto dto, CancellationToken ct = default)
    {
        var block = new Block
        {
            BlockName = dto.BlockName,
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

        block.BlockName = dto.BlockName;
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

        // Nếu đã đóng rồi thì bỏ qua
        if (block.Status == "Closed")
            return;

        block.Status = "Closed";

        await _repo.UpdateAsync(block, ct);
    }
}