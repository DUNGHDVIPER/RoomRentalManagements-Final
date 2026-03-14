using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Room.Public;

namespace WebCustomerBlazor.Services;

public interface ICustomerRoomCatalog
{
    Task<PagedResultDto<RoomPublicDto>> SearchAsync(FilterRoomDto filter, PagedRequestDto page);
    Task<RoomPublicDto?> GetByIdAsync(int id);
}
