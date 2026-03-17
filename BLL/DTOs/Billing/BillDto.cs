namespace BLL.DTOs.Billing;

public class BillDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int Period { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime DueDate { get; set; }
    public int Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? RoomName { get; set; }
    public List<BillItemDto> Items { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
}
