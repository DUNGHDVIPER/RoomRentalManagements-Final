using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities.Contracts;

public class ContractReminderLog
{
    [Key]
    public long Id { get; set; }

    public int ContractId { get; set; }

    [MaxLength(30)]
    public string RemindType { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateTime RemindAtDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Contract Contract { get; set; } = null!;
}