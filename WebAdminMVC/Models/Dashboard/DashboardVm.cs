namespace WebAdmin.MVC.Models.Dashboard;

public class DashboardVm
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public string TotalRevenue { get; set; } = string.Empty;
    public int PendingTasks { get; set; }
}