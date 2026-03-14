using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebAdmin.MVC.Models.Reports;

namespace WebAdmin.MVC.Controllers;

public class ReportsController : Controller
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, string? mode, CancellationToken ct)
    {
        var y = year ?? DateTime.Now.Year;
        int? m = (month is >= 1 and <= 12) ? month : null;

        var md = (mode ?? "month").Trim().ToLowerInvariant();
        if (md != "month" && md != "quarter" && md != "year") md = "month";

        // ✅ dto phải được khai báo trước khi dùng
        var dto = await _reports.GetRevenueDashboardAsync(y, ct);

        var vm = new RevenueDashboardVm
        {
            Year = y,
            Month = m,
            Mode = md,

            TotalRevenue = dto.TotalRevenue,
            TotalIssued = dto.TotalIssued,
            TotalDebt = dto.TotalDebt,
            CollectionRate = dto.CollectionRate,
            OccupiedRooms = dto.OccupiedRooms,
            TotalRooms = dto.TotalRooms,
            RevenueByMonth = dto.RevenueByMonth,
            IssuedByMonth = dto.IssuedByMonth,

            // ✅ thêm breakdown
            IssuedBreakdownByMonth = dto.IssuedBreakdownByMonth
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(int year, string? mode, int? month, CancellationToken ct)
    {
        var md = (mode ?? "month").Trim().ToLowerInvariant();
        if (md != "month" && md != "quarter" && md != "year") md = "month";

        int? m = (month is >= 1 and <= 12) ? month : null;
        if (md != "month") m = null; // quarter/year thì không cần month

        // reportName encode option để service parse
        var reportName = $"Revenue-{year}-{md}-{(m ?? 0)}";

        var bytes = await _reports.ExportExcelStubAsync(reportName, ct);
        var tag = md == "month" && m.HasValue ? $"-{m:00}" : $"-{md}";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"revenue-{year}{tag}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(int year, string? mode, int? month, CancellationToken ct)
    {
        var md = (mode ?? "month").Trim().ToLowerInvariant();
        if (md != "month" && md != "quarter" && md != "year") md = "month";

        int? m = (month is >= 1 and <= 12) ? month : null;
        if (md != "month") m = null;

        var reportName = $"Revenue-{year}-{md}-{(m ?? 0)}";

        var bytes = await _reports.ExportPdfStubAsync(reportName, ct);
        var tag = md == "month" && m.HasValue ? $"-{m:00}" : $"-{md}";
        return File(bytes, "application/pdf", $"revenue-{year}{tag}.pdf");
    }
}