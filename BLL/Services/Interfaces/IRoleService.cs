using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Identity;

namespace BLL.Services.Interfaces;

public interface IRoleService
{
    Task<PagedResultDto<RoleDto>> GetRolesAsync(PagedRequestDto req, CancellationToken ct = default);
    Task<RoleDto> CreateAsync(CreateRoleDto dto, CancellationToken ct = default);
    Task<RoleDto> UpdateAsync(string id, UpdateRoleDto dto, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
