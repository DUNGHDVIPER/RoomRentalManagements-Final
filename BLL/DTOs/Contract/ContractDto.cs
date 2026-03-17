public class ContractDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal Deposit { get; set; }
    public decimal Rent { get; set; }
    public int Status { get; set; }
    public bool IsActive { get; set; }

    // NEW
    public string DepositStatus { get; set; } = "Unpaid";
    public DateTime? DepositPaidAt { get; set; }
    public decimal? DepositPaidAmount { get; set; }
    public string? ContractCode { get; set; }
}
