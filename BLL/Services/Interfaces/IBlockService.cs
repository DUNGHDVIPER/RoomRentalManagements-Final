using BLL.DTOs.Property;

namespace BLL.Services.Interfaces;

public interface IBlockService
{
    Task<IReadOnlyList<BlockDto>> GetAllAsync(CancellationToken ct = default);

    Task<BlockDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<int> CreateAsync(BlockDto dto, CancellationToken ct = default);

    Task UpdateAsync(int id, BlockDto dto, CancellationToken ct = default);

    Task CloseAsync(int id, CancellationToken ct = default);
}