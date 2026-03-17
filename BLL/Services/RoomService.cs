using BLL.Common;
using BLL.DTOs;
using BLL.DTOs.Property;
using BLL.DTOs.Room;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.Property;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class RoomService : IRoomService
{
    private readonly AppDbContext _context;

    public RoomService(AppDbContext context)
    {
        _context = context;
    }

    // ================== ROOMS ==================

    public async Task<List<RoomDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Rooms
    .Include(r => r.Floor)
        .ThenInclude(f => f.Block)
    .Include(r => r.RoomAmenities)
    .AsNoTracking()
    .Select(r => new RoomDto
    {
        RoomId = r.RoomId,
        FloorId = r.FloorId,
        RoomCode = r.RoomCode,
        RoomName = r.RoomName,
        AreaM2 = r.AreaM2,
        MaxOccupants = r.MaxOccupants,
        Status = r.Status,
        CurrentBasePrice = r.CurrentBasePrice,

        BlockName = r.Floor.Block.BlockName,
        FloorName = r.Floor.FloorName,

        AmenityIds = r.RoomAmenities
            .Select(x => x.AmenityId)
            .ToList()
    })
    .ToListAsync(ct);

    }

    public async Task<RoomDto?> GetRoomDetailAsync(int roomId, CancellationToken ct = default)
    {
        var room = await _context.Rooms
            .Include(r => r.Floor)
                .ThenInclude(f => f.Block)
            .Include(r => r.RoomAmenities)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoomId == roomId, ct);

        if (room == null)
            return null;

        return new RoomDto
        {
            RoomId = room.RoomId,
            FloorId = room.FloorId,
            RoomCode = room.RoomCode,
            RoomName = room.RoomName,
            AreaM2 = room.AreaM2,
            MaxOccupants = room.MaxOccupants,
            Status = room.Status,
            CurrentBasePrice = room.CurrentBasePrice,
            Description = room.Description,

            FloorName = room.Floor?.FloorName,
            BlockName = room.Floor?.Block?.BlockName,

            AmenityIds = room.RoomAmenities
                .Select(x => x.AmenityId)
                .ToList()
        };
    }

    public async Task<RoomDto> CreateRoomAsync(CreateRoomDto dto, CancellationToken ct = default)
    {
        var code = dto.RoomCode.Trim().ToLower();

        var exists = await _context.Rooms
            .AnyAsync(x => x.RoomCode.ToLower() == code, ct);

        if (exists)
            throw new Exception("Room code already exists.");

        var room = new Room
        {
            FloorId = dto.FloorId,
            RoomCode = dto.RoomCode.Trim(),
            RoomName = dto.RoomName,
            AreaM2 = dto.AreaM2,
            MaxOccupants = dto.MaxOccupants,
            CurrentBasePrice = dto.CurrentBasePrice,
            Status = dto.Status,
            Description = dto.Description
        };

        _context.Rooms.Add(room);

        await _context.SaveChangesAsync(ct);

        if (dto.AmenityIds != null && dto.AmenityIds.Any())
        {
            await ValidateAmenities(dto.AmenityIds, ct);

            var amenities = dto.AmenityIds.Select(a => new RoomAmenity
            {
                RoomId = room.RoomId,
                AmenityId = a
            });

            await _context.RoomAmenities.AddRangeAsync(amenities, ct);
            await _context.SaveChangesAsync(ct);
        }

        return MapToDto(room);
    }

    public async Task<RoomDto> UpdateRoomAsync(int roomId, UpdateRoomDto dto, CancellationToken ct = default)
    {
        var room = await _context.Rooms
            .Include(r => r.RoomAmenities)
            .FirstOrDefaultAsync(r => r.RoomId == roomId, ct);

        var code = dto.RoomCode.Trim().ToLower();

        var exists = await _context.Rooms
            .AnyAsync(x => x.RoomCode.ToLower() == code, ct);

        if (exists)
            throw new Exception("Room code already exists.");

        if (room.CurrentBasePrice != dto.CurrentBasePrice)
        {
            var history = new RoomPricingHistory
            {
                RoomId = roomId,
                OldPrice = room.CurrentBasePrice,
                NewPrice = dto.CurrentBasePrice,
                ChangedAt = DateTime.UtcNow,
                Note = "Price updated"
            };

            _context.RoomPricingHistories.Add(history);
        }

        // FIX
        room.RoomCode = dto.RoomCode;
        room.RoomName = dto.RoomName;
        room.AreaM2 = dto.AreaM2;
        room.MaxOccupants = dto.MaxOccupants;
        room.Status = dto.Status;
        room.CurrentBasePrice = dto.CurrentBasePrice;
        room.Description = dto.Description;

        await _context.SaveChangesAsync(ct);

        if (dto.AmenityIds != null)
        {
            await ValidateAmenities(dto.AmenityIds, ct);
            await SetRoomAmenitiesAsync(roomId, dto.AmenityIds.ToArray(), ct);
        }

        return MapToDto(room);
    }
    public async Task DeleteRoomAsync(int roomId, CancellationToken ct = default)
    {
        // ===== Check contract =====
        var hasContract = await _context.Contracts
            .AnyAsync(x => x.RoomId == roomId, ct);

        if (hasContract)
            throw new Exception("Cannot delete room with contract.");

        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.RoomId == roomId, ct);

        if (room == null)
            return;

        _context.Rooms.Remove(room);

        await _context.SaveChangesAsync(ct);
    }

    // ================== AMENITIES ==================

    public async Task<List<AmenityDto>> GetAmenitiesAsync(CancellationToken ct = default)
    {
        return await _context.Amenities
            .AsNoTracking()
            .Select(a => new AmenityDto
            {
                AmenityId = a.Id,
                AmenityName = a.AmenityName
            })
            .ToListAsync(ct);
    }

    public async Task SetRoomAmenitiesAsync(int roomId, int[] amenityIds, CancellationToken ct = default)
    {
        var existing = await _context.RoomAmenities
            .Where(x => x.RoomId == roomId)
            .ToListAsync(ct);

        _context.RoomAmenities.RemoveRange(existing);

        var newAmenities = amenityIds.Select(id => new RoomAmenity
        {
            RoomId = roomId,
            AmenityId = id
        });

        await _context.RoomAmenities.AddRangeAsync(newAmenities, ct);

        await _context.SaveChangesAsync(ct);
    }

    private async Task ValidateAmenities(IEnumerable<int> amenityIds, CancellationToken ct)
    {
        var valid = await _context.Amenities
            .Where(x => amenityIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (valid.Count != amenityIds.Count())
            throw new Exception("Some amenities are invalid.");
    }

    // ================== PRICE HISTORY ==================

    public async Task AddRoomPriceHistoryAsync(int roomId, RoomPriceHistoryDto dto, CancellationToken ct = default)
    {
        var history = new RoomPricingHistory
        {
            RoomId = roomId,
            OldPrice = dto.OldPrice,
            NewPrice = dto.NewPrice,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = dto.ChangedByUserId,
            Note = dto.Note
        };

        _context.RoomPricingHistories.Add(history);

        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<RoomPriceHistoryDto>> GetRoomPriceHistoryAsync(int roomId, CancellationToken ct = default)
    {
        return await _context.RoomPricingHistories
            .Where(x => x.RoomId == roomId)
            .OrderByDescending(x => x.ChangedAt)
            .Select(x => new RoomPriceHistoryDto
            {
                OldPrice = x.OldPrice,
                NewPrice = x.NewPrice,
                ChangedAt = x.ChangedAt,
                ChangedByUserId = x.ChangedByUserId,
                Note = x.Note
            })
            .ToListAsync(ct);
    }

    // ================== NOT IMPLEMENTED ==================

    public Task<List<BlockDto>> GetBlocksAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<BlockDto> CreateBlockAsync(BlockDto dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<BlockDto> UpdateBlockAsync(int id, BlockDto dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task DeleteBlockAsync(int id, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<List<FloorDto>> GetFloorsByBlockAsync(int blockId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<PagedResultDto<RoomDto>> GetRoomsAsync(FilterRoomDto filter, CancellationToken ct = default)
        => throw new NotImplementedException();
    public async Task AddRoomImagesAsync(int roomId, string[] imageUrls, CancellationToken ct = default)
    {
        var images = imageUrls.Select((url, index) => new RoomImage
        {
            RoomId = roomId,
            ImageUrl = url,
            IsPrimary = index == 0,
         
        });

        await _context.RoomImages.AddRangeAsync(images, ct);
        await _context.SaveChangesAsync(ct);
    }


    public async Task RemoveRoomImageAsync(int imageId, CancellationToken ct = default)
    {
        var image = await _context.RoomImages
            .FirstOrDefaultAsync(x => x.ImageId == imageId, ct);

        if (image == null)
            return;

        _context.RoomImages.Remove(image);

        await _context.SaveChangesAsync(ct);
    }

    // ================== MAPPER ==================

    private static RoomDto MapToDto(Room r) => new()
    {
        RoomId = r.RoomId,
        FloorId = r.FloorId,
        RoomCode = r.RoomCode,
        RoomName = r.RoomName,
        AreaM2 = r.AreaM2,
        MaxOccupants = r.MaxOccupants,
        Status = r.Status,
        CurrentBasePrice = r.CurrentBasePrice,
        Description = r.Description,

        AmenityIds = r.RoomAmenities?
    .Select(x => x.AmenityId)
    .ToList() ?? new List<int>()
    };

}
