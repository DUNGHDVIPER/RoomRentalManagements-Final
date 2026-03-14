namespace WebAdmin.MVC.Models.Billing;

public class BillDetailsVm
{
    public int Id { get; set; }
    public string RoomName { get; set; } = null!;
    public string Month { get; set; } = null!;
    public decimal Total { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow.AddDays(-10);
    public List<PaymentHistoryItemVm> Payments { get; set; } = new();
    public List<BillItemLineVm> Items { get; set; } = new();
}
