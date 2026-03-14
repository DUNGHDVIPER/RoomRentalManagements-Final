namespace BLL.DTOs.Contract;

public class ContractVersionItemDto
{
    public long VersionId { get; set; }
    public int VersionNumber { get; set; }
    public DateTime ChangedAt { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? ChangeNote { get; set; }
    public string SnapshotJson { get; set; } = "";
}