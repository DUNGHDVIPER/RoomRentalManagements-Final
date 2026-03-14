namespace BLL.DTOs.Contract;

public class UpdateDepositDto
{
    public int ContractId { get; set; }

    public decimal DepositAmount { get; set; } // new required deposit
    public string DepositStatus { get; set; } = "Unpaid"; // Unpaid/Paid/Refunded/Forfeit

    public decimal? PaidAmount { get; set; } // if status Paid/Refund/Forfeit can set amount
    public DateTime? PaidAt { get; set; }    // required when Paid (and optionally for Refund/Forfeit)

    public string? Note { get; set; }
}
