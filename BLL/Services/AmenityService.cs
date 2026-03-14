using BLL.DTOs.Room;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class AmenityService : IAmenityService
{
    private readonly AppDbContext _context;

    public AmenityService(AppDbContext context)
    {
        _context = context;
    }

    // ===================== GET ALL =====================
    public async Task<List<AmenityDto>> GetAllAsync()
    {
        return await _context.Amenities
            .Select(x => new AmenityDto
            {
                AmenityId = x.Id,
                AmenityName = x.AmenityName
            })
            .ToListAsync();
    }

    // ===================== GET BY ID =====================
    public async Task<AmenityDto?> GetByIdAsync(int id)
    {
        return await _context.Amenities
            .Where(x => x.Id == id)
            .Select(x => new AmenityDto
            {
                AmenityId = x.Id,
                AmenityName = x.AmenityName
            })
            .FirstOrDefaultAsync();
    }

    // ===================== CREATE =====================
    public async Task CreateAsync(AmenityDto dto)
    {
        var amenity = new Amenity
        {
            AmenityName = dto.AmenityName
        };

        _context.Amenities.Add(amenity);
        await _context.SaveChangesAsync();
    }

    // ===================== UPDATE =====================
    public async Task UpdateAsync(AmenityDto dto)
    {
        var amenity = await _context.Amenities.FindAsync(dto.AmenityId);

        if (amenity == null)
            return;

        amenity.AmenityName = dto.AmenityName;

        await _context.SaveChangesAsync();
    }

    // ===================== DELETE =====================
    public async Task DeleteAsync(int id)
    {
        var amenity = await _context.Amenities.FindAsync(id);

        if (amenity == null)
            return;

        // Lấy danh sách RoomAmenities đang dùng Amenity này
        var roomAmenities = await _context.RoomAmenities
            .Where(x => x.AmenityId == id)
            .ToListAsync();

        // Xóa trước
        if (roomAmenities.Any())
        {
            _context.RoomAmenities.RemoveRange(roomAmenities);
        }

        // Xóa Amenity
        _context.Amenities.Remove(amenity);

        await _context.SaveChangesAsync();
    }
}