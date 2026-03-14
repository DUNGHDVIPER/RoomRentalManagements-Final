namespace BLL.DTOs.Report;

public class FeeBreakdownDto
{
    public decimal Rent { get; set; }
    public decimal Electric { get; set; }
    public decimal Water { get; set; }
    public decimal Other { get; set; }
}

public class RevenueReportDto
{
    public int Year { get; set; }

    public decimal TotalRevenue { get; set; }
    public decimal TotalIssued { get; set; }
    public decimal TotalDebt { get; set; }
    public double CollectionRate { get; set; }

    public Dictionary<int, decimal> RevenueByMonth { get; set; } = new(); // collected
    public Dictionary<int, decimal> IssuedByMonth { get; set; } = new();  // issued

    public int OccupiedRooms { get; set; }
    public int TotalRooms { get; set; }

    // ✅ QUAN TRỌNG: thêm dòng này
    public Dictionary<int, FeeBreakdownDto> IssuedBreakdownByMonth { get; set; } = new();
}