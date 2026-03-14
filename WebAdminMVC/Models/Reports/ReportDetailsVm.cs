namespace WebAdmin.MVC.Models.Reports;

public class ReportDetailsVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Period { get; set; } = null!;
    public string Type { get; set; } = null!;
    public DateTime GeneratedAtUtc { get; set; }
    public string Notes { get; set; } = "Stub report preview (FE-only).";
}
