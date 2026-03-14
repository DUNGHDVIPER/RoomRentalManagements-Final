namespace BLL.DTOs.Ticket;

public class UpdateTicketStatusDto
{
    public int TicketId { get; set; }
    public int NewStatus { get; set; } // TicketStatus int
}
