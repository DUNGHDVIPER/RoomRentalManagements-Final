namespace BLL.DTOs.Contract;

public class CreateContractDto
{
    public int RoomId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Deposit { get; set; }
    public decimal Rent { get; set; }

    public bool ActivateNow { get; set; } = true;
}
