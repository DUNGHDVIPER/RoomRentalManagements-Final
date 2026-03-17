namespace BLL.DTOs.Ticket;

public class TicketDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int? TenantId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int Status { get; set; }
}
