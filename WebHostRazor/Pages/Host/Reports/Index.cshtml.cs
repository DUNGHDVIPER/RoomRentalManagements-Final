using BLL.DTOs.Report;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebRazor.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly IReportService _reports;

    public IndexModel(IReportService reports)
    {
        _reports = reports;
    }

    public RevenueReportDto Vm { get; set; } = new();

    public async Task OnGetAsync(int? year, int? month, string? mode, CancellationToken ct)
    {
        var y = year ?? DateTime.Now.Year;

        Vm = await _reports.GetRevenueDashboardAsync(y, ct);
    }
}