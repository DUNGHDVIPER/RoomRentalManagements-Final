using BLL.DTOs.Room.Public;
using DAL.Data;
using DAL.Entities.Common;
using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class CustomerRoomService
{
    private readonly AppDbContext _context;

    public CustomerRoomService(AppDbContext context)
    {
        _context = context;
    }

    // =============================
    // LIST ROOMS
    // =============================
    public async Task<List<RoomPublicDto>> GetRoomsAsync(
      string? search = null,
      decimal? maxPrice = null,
      double? minArea = null,
      string? blockName = null,
      string? floorName = null)
    {
        var query = _context.Rooms
            .Include(r => r.RoomImages)
            .Include(r => r.RoomAmenities)
                .ThenInclude(x => x.Amenity)
            .Include(r => r.Floor)
                .ThenInclude(f => f.Block)
            .Where(r => r.Status == RoomStatus.Available)
            .AsQueryable();

        // SEARCH
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                (r.RoomName != null && r.RoomName.Contains(search)) ||
                r.RoomCode.Contains(search));
        }

        // FILTER PRICE
        if (maxPrice.HasValue)
        {
            query = query.Where(r => r.CurrentBasePrice <= maxPrice.Value);
        }

        // FILTER AREA
        if (minArea.HasValue)
        {
            query = query.Where(r => r.AreaM2 >= (decimal)minArea.Value);
        }

        // FILTER BLOCK
        if (!string.IsNullOrWhiteSpace(blockName))
        {
            query = query.Where(r =>
                r.Floor != null &&
                r.Floor.Block != null &&
                r.Floor.Block.BlockName == blockName);
        }

        // FILTER FLOOR
        if (!string.IsNullOrWhiteSpace(floorName))
        {
            query = query.Where(r =>
                r.Floor != null &&
                r.Floor.FloorName == floorName);
        }

        var rooms = await query
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return rooms.Select(MapToPublicDto).ToList();
    }
    // =============================
    // ROOM DETAIL
    // =============================
    public async Task<RoomPublicDto?> GetRoomAsync(int roomId)
    {
        var room = await _context.Rooms
            .Include(r => r.RoomImages)
            .Include(r => r.RoomAmenities)
                .ThenInclude(x => x.Amenity)
            .Include(r => r.Floor)
                .ThenInclude(f => f.Block)
            .Where(r => r.RoomId == roomId && r.Status == RoomStatus.Available)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (room == null)
            return null;

        return MapToPublicDto(room);
    }
    // =============================
    // GET FILTER OPTIONS
    // =============================
    public async Task<RoomFilterDto> GetRoomFiltersAsync()
    {
        var blocks = await _context.Blocks
            .Select(b => b.BlockName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var floors = await _context.Floors
            .Select(f => f.FloorName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return new RoomFilterDto
        {
            Blocks = blocks,
            Floors = floors
        };
    }
    // =============================
    // MAPPER
    // =============================
    private RoomPublicDto MapToPublicDto(Room r)
    {
        return new RoomPublicDto
        {
            Id = r.RoomId,

            Name = !string.IsNullOrWhiteSpace(r.RoomName)
                ? r.RoomName
                : r.RoomCode,

            District = r.Floor?.Block?.BlockName ?? "",

            Price = r.CurrentBasePrice,

            Area = (double)(r.AreaM2 ?? 0),

            PostedAgo = GetPostedAgo(r.CreatedAt),

            Images = r.RoomImages.Any()
                ? r.RoomImages.Select(i => new RoomImagePublicDto
                {
                    Url = i.ImageUrl
                }).ToList()
                : new List<RoomImagePublicDto>
                {
                new RoomImagePublicDto
                {
                    Url = "/images/no-image.jpg"
                }
                },

            Amenities = r.RoomAmenities
                .Select(a => new AmenityPublicDto
                {
                    Name = a.Amenity.AmenityName
                }).ToList(),

            Badges = BuildBadges(r)
        };
    }

    // =============================
    // BADGES
    // =============================
    private List<string> BuildBadges(DAL.Entities.Property.Room r)
    {
        var badges = new List<string>();

        if (r.AreaM2.HasValue && r.AreaM2.Value >= 30)
            badges.Add("Rộng");

        if (r.MaxOccupants >= 3)
            badges.Add("Ở được nhiều người");

        if (r.CurrentBasePrice < 3000000)
            badges.Add("Giá tốt");

        return badges;
    }

    // =============================
    // TIME FORMAT
    // =============================
    private string GetPostedAgo(DateTime created)
    {
        var diff = DateTime.Now - created;

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes} phút trước";

        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} giờ trước";

        return $"{(int)diff.TotalDays} ngày trước";
    }
}