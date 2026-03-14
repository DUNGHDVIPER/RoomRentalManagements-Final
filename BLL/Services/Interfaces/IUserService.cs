using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Identity;

namespace BLL.Services.Interfaces;

public interface IUserService
{
    Task<PagedResultDto<UserDto>> GetUsersAsync(PagedRequestDto req, CancellationToken ct = default);
    Task<UserDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(string id, UpdateUserDto dto, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);

    Task LockAsync(string id, CancellationToken ct = default);
    Task UnlockAsync(string id, CancellationToken ct = default);
}
