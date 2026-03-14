namespace WebAdmin.MVC.Models.Billing;

public class BillListItemVm
{
    public int Id { get; set; }
    public string RoomName { get; set; } = null!;
    public string Month { get; set; } = null!; // 2026-02
    public decimal Total { get; set; }
    public string Status { get; set; } = "Unpaid"; // Unpaid/Paid/Overdue
}
