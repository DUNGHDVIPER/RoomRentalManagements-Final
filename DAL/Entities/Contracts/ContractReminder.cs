using System.ComponentModel.DataAnnotations;

namespace DAL.Entities.Contracts;

public class ContractReminder
{
    [Key]
    public int Id { get; set; }

    public int ContractId { get; set; }

    [MaxLength(30)]
    public string Type { get; set; } = null!;

    public DateTime RemindAt { get; set; }

    public bool IsSent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Contract Contract { get; set; } = null!;
}