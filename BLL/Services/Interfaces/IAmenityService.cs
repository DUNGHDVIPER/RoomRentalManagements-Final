using BLL.DTOs.Room;

namespace BLL.Services.Interfaces;

public interface IAmenityService
{
    Task<List<AmenityDto>> GetAllAsync();
    Task<AmenityDto?> GetByIdAsync(int id);
    Task CreateAsync(AmenityDto dto);
    Task UpdateAsync(AmenityDto dto);
    Task DeleteAsync(int id);
}