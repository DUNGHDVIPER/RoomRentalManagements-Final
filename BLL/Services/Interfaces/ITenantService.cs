using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Tenant;

namespace BLL.Services.Interfaces;

public interface ITenantService
{
    Task<
        PagedResultDto<TenantDto>> GetTenantsAsync(PagedRequestDto req, CancellationToken ct = default);
    Task<TenantDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TenantDto> CreateAsync(CreateTenantDto dto, CancellationToken ct = default);
    Task<TenantDto> UpdateAsync(int id, UpdateTenantDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    // CCCD/Docs
    Task<List<TenantIdDocDto>> GetIdDocsAsync(int tenantId, CancellationToken ct = default);
    Task<TenantIdDocDto> AddIdDocAsync(int tenantId, TenantIdDocDto dto, CancellationToken ct = default);
    Task RemoveIdDocAsync(int docId, CancellationToken ct = default);

    // Blacklist (stub field/logic later)
    Task BlacklistAsync(int tenantId, string reason, CancellationToken ct = default);
    Task UnBlacklistAsync(int tenantId, CancellationToken ct = default);

    // Residents & Stay history
    Task<List<StayHistoryDto>> GetStayHistoryAsync(int tenantId, CancellationToken ct = default);
}
