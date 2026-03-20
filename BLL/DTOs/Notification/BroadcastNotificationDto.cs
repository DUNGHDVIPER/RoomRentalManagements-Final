using DAL.Entities.Common;

public class BroadcastNotificationDto
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public SourceType SourceType { get; set; }

    // ✅ FIX: thêm đủ field
    public List<int>? TenantIds { get; set; }
    public List<int>? ContractIds { get; set; }

    public int? BlockId { get; set; }
    public int? FloorId { get; set; }

    public bool SendToHost { get; set; }
}