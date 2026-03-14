using DAL.Entities.Motel;
using System.ComponentModel.DataAnnotations;

namespace DAL.Entities.Contracts;

public class ContractVersion
{
    [Key]
    public long VersionId { get; set; } // DB: IDENTITY

    public int ContractId { get; set; }
    public int VersionNumber { get; set; }
    public DateTime ChangedAt { get; set; }
    public int? ChangedByUserId { get; set; }

    [MaxLength(255)]
    public string? ChangeNote { get; set; }

    public string SnapshotJson { get; set; } = null!;

    public Contract Contract { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}