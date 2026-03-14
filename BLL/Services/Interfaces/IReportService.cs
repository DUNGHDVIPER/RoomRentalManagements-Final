using BLL.DTOs.Report;

namespace BLL.Services.Interfaces;

public interface IReportService
{
    Task<RevenueReportDto> GetRevenueDashboardAsync(int year, CancellationToken ct = default);
    Task<ProfitReportDto> GetProfitDashboardAsync(int year, CancellationToken ct = default);

    Task<byte[]> ExportExcelStubAsync(string reportName, CancellationToken ct = default);
    Task<byte[]> ExportPdfStubAsync(string reportName, CancellationToken ct = default);
    Task GetRevenueAsync();
}
