namespace BLL.DTOs.Billing;

public class RecordPaymentDto
{
    public int BillId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash";
    public string? TransactionRef { get; set; }
    public DateTime? PaidAt { get; set; }
}
