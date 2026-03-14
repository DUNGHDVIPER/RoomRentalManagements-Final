namespace BLL.DTOs.Billing;

public class BillItemDto
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public string Name { get; set; } = null!;
    public decimal Amount { get; set; }
    public int? ExtraFeeId { get; set; }
}
