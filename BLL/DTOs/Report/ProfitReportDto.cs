namespace BLL.DTOs.Report;

public class ProfitReportDto
{
    public int Year { get; set; }
    public decimal TotalProfit { get; set; }
    public Dictionary<int, decimal> ProfitByMonth { get; set; } = new(); // month -> profit
}
