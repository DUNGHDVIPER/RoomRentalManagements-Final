using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Ticket;
namespace BLL.Services.Interfaces;
public interface ITicketService
{
    Task<bool> CreateAsync(CreateTicketDto dto, string email);

    Task<TicketDto> AssignAsync(AssignTicketDto dto, CancellationToken ct = default);

    Task<TicketDto> UpdateStatusAsync(UpdateTicketStatusDto dto, CancellationToken ct = default);

    Task<PagedResultDto<TicketDto>> GetTicketsAsync(PagedRequestDto req, CancellationToken ct = default);
}