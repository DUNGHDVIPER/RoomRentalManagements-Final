namespace BLL.DTOs.Contract;

public class RenewContractDto
{
    public int ContractId { get; set; }
    public DateTime NewStartDate { get; set; }
    public DateTime NewEndDate { get; set; }
    public decimal NewRent { get; set; }
    public decimal NewDeposit { get; set; }
    public string? Reason { get; set; }
    public bool RequireStartNextDay { get; set; } = true; // default: EndDate + 1 day
}
