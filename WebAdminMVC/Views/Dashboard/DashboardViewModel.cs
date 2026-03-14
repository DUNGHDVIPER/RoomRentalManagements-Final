namespace WebAdminMVC.ViewModels.Dashboard;

public class DashboardViewModel
{
    // KPI cards
    public decimal TotalRevenue { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }

    // Chart / Table
    public IReadOnlyList<RevenueByMonthItem> RevenueByMonth { get; set; }
        = [];
}

public class RevenueByMonthItem
{
    public string Month { get; set; } = null!;
    public decimal Revenue { get; set; }
}
