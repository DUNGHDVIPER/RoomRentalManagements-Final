using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Identity;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class UserService : IUserService
{
    public Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<UserDto> GetByIdAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<PagedResultDto<UserDto>> GetUsersAsync(PagedRequestDto req, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task LockAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UnlockAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<UserDto> UpdateAsync(string id, UpdateUserDto dto, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
