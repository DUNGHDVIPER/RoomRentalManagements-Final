namespace BLL.DTOs.Billing;

public class PaymentDto
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = null!;
    public int Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? TransactionRef { get; set; }
}
