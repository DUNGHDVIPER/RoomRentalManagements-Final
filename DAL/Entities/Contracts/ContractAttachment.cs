using System.ComponentModel.DataAnnotations;

namespace DAL.Entities.Contracts;

public class ContractAttachment
{
    [Key]
    public long AttachmentId { get; set; }

    public int ContractId { get; set; }

    [MaxLength(500)]
    public string FileUrl { get; set; } = null!;

    [MaxLength(200)]
    public string? FileName { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int? UploadedByUserId { get; set; }

    public Contract Contract { get; set; } = null!;
}