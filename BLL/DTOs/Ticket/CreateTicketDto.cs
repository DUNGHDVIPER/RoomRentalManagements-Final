namespace BLL.DTOs.Ticket;

public class CreateTicketDto
{
    public int RoomId { get; set; }
    public int? TenantId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
}
