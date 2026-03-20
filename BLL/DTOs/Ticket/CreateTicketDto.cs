using DAL.Entities.Common;

public class CreateTicketDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public TicketCategory Category { get; set; }
    public int RoomId { get; set; }

    // ❌ XÓA CÁI NÀY
    // public int TenantId { get; set; }
}