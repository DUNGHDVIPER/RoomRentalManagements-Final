namespace BLL.DTOs.Tenant;

public class StayHistoryDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int TenantId { get; set; }
    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public string? Note { get; set; }
}
