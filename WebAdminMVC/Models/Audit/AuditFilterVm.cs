namespace WebAdmin.MVC.Models.Audit;

public class AuditFilterVm
{
    public string? Keyword { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
