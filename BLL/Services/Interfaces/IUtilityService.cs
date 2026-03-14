using BLL.DTOs.Utility;

namespace BLL.Services.Interfaces;

public interface IUtilityService
{
    Task<UtilityPriceDto> GetCurrentPriceAsync(CancellationToken ct = default);
    Task SetPriceAsync(UtilityPriceDto dto, CancellationToken ct = default);

    Task BulkUpsertReadingsAsync(BulkUtilityReadingDto dto, CancellationToken ct = default);
    Task<UtilityChargeResultDto> CalculateChargesAsync(int roomId, int period, CancellationToken ct = default);
}
