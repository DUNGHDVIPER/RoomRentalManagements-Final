using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Identity;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class RoleService : IRoleService
{
    public Task<RoleDto> CreateAsync(CreateRoleDto dto, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<PagedResultDto<RoleDto>> GetRolesAsync(PagedRequestDto req, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<RoleDto> UpdateAsync(string id, UpdateRoleDto dto, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
