using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebAdminMVC.ViewModels.Dashboard;

namespace WebAdminMVC.Controllers;

public class AdminDashboardController : Controller
{
    private readonly IReportService _reportService;

    public AdminDashboardController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index()
    {
        // Fix for CS0023: Ensure the correct method is called and the result is awaited
        var revenueReport = await _reportService.GetRevenueDashboardAsync(DateTime.Now.Year);

        // Fix for IDE0090: Simplify object initialization
        var vm = new DashboardViewModel
        {
            TotalRevenue = revenueReport.TotalRevenue,
            TotalRooms = revenueReport.TotalRooms,
            OccupiedRooms = revenueReport.OccupiedRooms,
            RevenueByMonth = (IReadOnlyList<RevenueByMonthItem>)revenueReport.RevenueByMonth
        };

        return View(vm);
    }
}
