using BLL.DTOs.Property;

public interface IFloorService
{
    Task<List<FloorListVm>> GetByBlockAsync(int blockId);
    Task<FloorDto?> GetByIdAsync(int id);
    Task CreateAsync(FloorDto dto);
    Task UpdateAsync(FloorDto dto);
    Task DeleteAsync(int id);
}