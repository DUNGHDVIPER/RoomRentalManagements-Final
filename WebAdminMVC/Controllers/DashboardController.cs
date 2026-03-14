using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAdmin.MVC.Models.Dashboard;

namespace WebAdmin.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class DashboardController : Controller
{
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ILogger<DashboardController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        _logger.LogInformation("Dashboard accessed by user: {User}", User.Identity?.Name);

        // Tạo model với dữ liệu mặc định nếu không có model
        var model = new DashboardVm
        {
            TotalUsers = 245,
            ActiveUsers = 198,
            TotalRevenue = "$45,230",
            PendingTasks = 12
        };

        return View(model);
    }
}