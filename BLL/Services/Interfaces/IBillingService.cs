using BLL.DTOs.Billing;
using Microsoft.AspNetCore.Mvc.Rendering;
using DAL.Entities.Billing;

namespace BLL.Services.Interfaces;

public interface IBillingService
{
    Task<List<BillDto>> GetBillsAsync(string? q, string? status, string? month, CancellationToken ct);
    Task<BillDto?> GetBillAsync(int id, CancellationToken ct);

    Task<List<SelectListItem>> GetActiveRoomOptionsAsync(CancellationToken ct);
    Task<List<ExtraFee>> GetActiveExtraFeesAsync(CancellationToken ct);

    Task<(bool Ok, string? Error)> CreateBillAsync(string roomNo, string month, string uiStatus, decimal total, CancellationToken ct);
    Task<(bool Ok, string? Error)> UpdateBillAsync(int id, string month, string uiStatus, decimal total, CancellationToken ct);

    Task<(bool Ok, string? Error)> DeleteBillAsync(int id, CancellationToken ct);

    Task<(bool Ok, string? Error)> RecordPaymentAsync(RecordPaymentDto dto, CancellationToken ct);

    Task<(bool Ok, string? Error, int Created, int Skipped)> GenerateBillsAsync(GenerateBillsRequestDto req, List<int> extraFeeIds, bool includeRent, bool includeUtilities, CancellationToken ct);
}