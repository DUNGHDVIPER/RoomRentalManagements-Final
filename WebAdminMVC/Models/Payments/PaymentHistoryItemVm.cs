namespace WebAdmin.MVC.Models.Billing;

public class PaymentHistoryItemVm
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash";
    public string Status { get; set; } = "Pending";
    public DateTime? PaidAtUtc { get; set; }
    public string? TransactionRef { get; set; }
}