namespace WebAdmin.MVC.Models.Reports;

public class ReportListItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Period { get; set; } = null!; // e.g. 2026-Q1
    public string Type { get; set; } = "Revenue";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
