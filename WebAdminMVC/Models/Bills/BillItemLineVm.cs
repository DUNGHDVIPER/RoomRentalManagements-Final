namespace WebAdmin.MVC.Models.Billing;

public class BillItemLineVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Amount { get; set; }
    public int? ExtraFeeId { get; set; }
}