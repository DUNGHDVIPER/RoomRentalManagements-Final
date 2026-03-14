using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Room.Public;

namespace WebCustomerBlazor.Services;

public class CustomerRoomCatalog : ICustomerRoomCatalog
{
    private static List<RoomPublicDto> Data => new()
    {
        new RoomPublicDto
        {
            Id = 1,
            Name = "Studio District 1",
            City = "Hồ Chí Minh",
            District = "District 1",
            Area = 28,
            Price = 8500000,
            PostedAgo = "2 hours ago",
            Badges = new() { "New", "Hot" },
            Images = new() { new RoomImagePublicDto { Url = "https://picsum.photos/seed/r1/800/600" } },
            Amenities = new() { new AmenityPublicDto { Name = "Wifi" }, new AmenityPublicDto { Name = "Aircon" } }
        },
        new RoomPublicDto
        {
            Id = 2,
            Name = "Luxury Suite - Landmark 81",
            City = "Hồ Chí Minh",
            District = "Bình Thạnh",
            Area = 45,
            Price = 15000000,
            PostedAgo = "1 day ago",
            Badges = new() { "Featured" },
            Images = new() { new RoomImagePublicDto { Url = "https://picsum.photos/seed/r2/800/600" } },
            Amenities = new() { new AmenityPublicDto { Name = "Parking" }, new AmenityPublicDto { Name = "Gym" } }
        }
    };

    public Task<PagedResultDto<RoomPublicDto>> SearchAsync(FilterRoomDto filter, PagedRequestDto page)
    {
        var q = Data.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            q = q.Where(x => x.Name.Contains(filter.Keyword, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.City))
            q = q.Where(x => x.City == filter.City);

        var total = q.Count();

        var items = q
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToList();

        // Fix: Use the constructor of PagedResultDto with the required parameters
        PagedResultDto<RoomPublicDto> res = new(items, total, page.PageNumber, page.PageSize);

        return Task.FromResult(res);
    }

    public Task<RoomPublicDto?> GetByIdAsync(int id)
        => Task.FromResult(Data.FirstOrDefault(x => x.Id == id));
}
