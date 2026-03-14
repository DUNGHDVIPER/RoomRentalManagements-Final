namespace WebAdmin.MVC.Models.Billing;

public class PaymentCreateVm
{
    public int BillId { get; set; }

    public string RoomName { get; set; } = "";
    public string Month { get; set; } = "";

    public decimal BillTotal { get; set; }
    public decimal PaidSoFar { get; set; }
    public decimal Remaining => BillTotal - PaidSoFar;

    public decimal Amount { get; set; }

    public string Method { get; set; } = "Cash";
    public string? TransactionRef { get; set; }
}