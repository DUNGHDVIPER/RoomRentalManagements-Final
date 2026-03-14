using BLL.DTOs.Report;

namespace WebAdmin.MVC.Models.Reports;

public class RevenueDashboardVm
{
    public int Year { get; set; }

    // ✅ THÊM MỚI: tháng đang chọn (1..12), null = xem cả năm
    public int? Month { get; set; }

    public decimal TotalRevenue { get; set; }
    public int OccupiedRooms { get; set; }
    public int TotalRooms { get; set; }
    public Dictionary<int, decimal> RevenueByMonth { get; set; } = new();

    public decimal TotalIssued { get; set; }
    public decimal TotalDebt { get; set; }
    public double CollectionRate { get; set; }

    public Dictionary<int, decimal> IssuedByMonth { get; set; } = new();
    public string Mode { get; set; } = "month"; // month | quarter | year

    public Dictionary<int, FeeBreakdownDto> IssuedBreakdownByMonth { get; set; } = new();


}