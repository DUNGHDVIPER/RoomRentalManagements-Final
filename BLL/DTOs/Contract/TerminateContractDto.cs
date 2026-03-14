namespace BLL.DTOs.Contract;

public class TerminateContractDto
{
    public int ContractId { get; set; }
    public DateTime TerminateDate { get; set; }
    public string? Reason { get; set; }
}
