namespace BLL.DTOs.Ticket;

public class AssignTicketDto
{
    public int TicketId { get; set; }
    public string AssignedToUserId { get; set; } = null!; // stub
}
