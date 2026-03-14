namespace BLL.DTOs.Contract;

public class TenantContractDetailsDto
{
    public int ContractId { get; set; }
    public string ContractCode { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsActive { get; set; }

    public int RoomId { get; set; }
    public string RoomCode { get; set; } = "";
    public string RoomName { get; set; } = "";

    public int TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public string TenantEmail { get; set; } = "";

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Rent { get; set; }
    public decimal Deposit { get; set; }
    public string DepositStatus { get; set; } = "Unpaid";
    public DateTime? DepositPaidAt { get; set; }
    public decimal? DepositPaidAmount { get; set; }
    public string? Note { get; set; }

    public List<ContractAttachmentDto> Attachments { get; set; } = new();
}