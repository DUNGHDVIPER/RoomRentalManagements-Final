namespace WebAdmin.MVC.Models.Audit;

public class AuditListItemVm
{
    public long Id { get; set; }
    public string TimeUtc { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string Entity { get; set; } = null!;
    public string User { get; set; } = null!;
}
