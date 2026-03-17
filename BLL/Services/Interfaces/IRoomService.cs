using BLL.Common;
using BLL.DTOs.Room;
using BLL.Dtos;


namespace BLL.Services.Interfaces;

public interface IRoomService
{
    // Blocks
    Task<List<BlockDto>> GetBlocksAsync(CancellationToken ct = default);
    Task<BlockDto> CreateBlockAsync(BlockDto dto, CancellationToken ct = default);
    Task<BlockDto> UpdateBlockAsync(int id, BlockDto dto, CancellationToken ct = default);
    Task DeleteBlockAsync(int id, CancellationToken ct = default);

    // Floors
    Task<List<FloorDto>> GetFloorsByBlockAsync(int blockId, CancellationToken ct = default);

    // Rooms CRUD + filter/paging
    Task<PagedResultDto<RoomDto>> GetRoomsAsync(Common.FilterRoomDto filter, CancellationToken ct = default);
    Task<RoomDto> GetRoomDetailAsync(int roomId, CancellationToken ct = default);
    Task<RoomDto> CreateRoomAsync(CreateRoomDto dto, CancellationToken ct = default);
    Task<RoomDto> UpdateRoomAsync(int roomId, UpdateRoomDto dto, CancellationToken ct = default);
    Task DeleteRoomAsync(int roomId, CancellationToken ct = default);

    // Images
    Task AddRoomImagesAsync(int roomId, string[] imageUrls, CancellationToken ct = default);
    Task RemoveRoomImageAsync(int imageId, CancellationToken ct = default);

    // Amenities
    Task<List<AmenityDto>> GetAmenitiesAsync(CancellationToken ct = default);
    Task SetRoomAmenitiesAsync(int roomId, int[] amenityIds, CancellationToken ct = default);

    // Pricing history
    Task AddRoomPriceHistoryAsync(int roomId, RoomPriceHistoryDto dto, CancellationToken ct = default);
    Task<List<RoomPriceHistoryDto>> GetRoomPriceHistoryAsync(int roomId, CancellationToken ct = default);
    //Task<string?> GetAllAsync();
    Task<List<RoomDto>> GetAllAsync(CancellationToken ct = default);
}
