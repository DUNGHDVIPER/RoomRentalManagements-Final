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
        var name = dto.AmenityName.Trim();

        var exists = await _context.Amenities
            .AnyAsync(x => x.AmenityName.ToLower() == name.ToLower());

        if (exists)
            throw new Exception("Amenity already exists.");

        var amenity = new Amenity
        {
            AmenityName = name
        };

        _context.Amenities.Add(amenity);
        await _context.SaveChangesAsync();
    }


    // ===================== UPDATE =====================
    public async Task UpdateAsync(AmenityDto dto)
    {
        var amenity = await _context.Amenities.FindAsync(dto.AmenityId);

        if (amenity == null)
            throw new Exception("Amenity not found.");

        var name = dto.AmenityName.Trim();

        var exists = await _context.Amenities
            .AnyAsync(x =>
                x.Id != dto.AmenityId &&
                x.AmenityName.ToLower() == name.ToLower());

        if (exists)
            throw new Exception("Amenity already exists.");

        amenity.AmenityName = name;

        await _context.SaveChangesAsync();
    }


    // ===================== DELETE =====================
    public async Task DeleteAsync(int id)
    {
        var amenity = await _context.Amenities.FindAsync(id);

        if (amenity == null)
            throw new Exception("Amenity not found.");

        var isUsed = await _context.RoomAmenities
            .AnyAsync(x => x.AmenityId == id);

        if (isUsed)
            throw new Exception("Cannot delete amenity because it is used by rooms.");

        _context.Amenities.Remove(amenity);

        await _context.SaveChangesAsync();
    }

}