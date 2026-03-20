using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Tenant;
using DAL.Entities.Tenanting;

namespace BLL.Services.Interfaces;

public interface ITenantService
{
    Task<PagedResultDto<TenantDto>> GetTenantsAsync(PagedRequestDto req, CancellationToken ct = default);
    Task<TenantDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TenantDto> CreateAsync(CreateTenantDto dto, CancellationToken ct = default);
    Task<TenantDto> UpdateAsync(int id, UpdateTenantDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id);

    Task BlacklistAsync(int tenantId, string reason, CancellationToken ct = default);
    Task UnBlacklistAsync(int tenantId, CancellationToken ct = default);

    Task<Tenant?> GetByEmailAsync(string email);
}