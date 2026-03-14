namespace BLL.DTOs.Contract;

public class ContractAttachmentDto
{
    public long AttachmentId { get; set; }
    public string FileName { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public DateTime UploadedAt { get; set; }

    public bool IsPdf =>
        FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public bool IsImage =>
        FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
        FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
        FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
        FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
}