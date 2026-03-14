using BLL.DTOs.Ticket;
using BLL.DTOs.Common;
using BLL.Common;
using BLL.Services.Interfaces;

namespace BLL.Services;

public class TicketService : ITicketService
{
    public Task<TicketDto> CreateAsync(CreateTicketDto dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TicketDto> AssignAsync(AssignTicketDto dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TicketDto> UpdateStatusAsync(UpdateTicketStatusDto dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<PagedResultDto<TicketDto>> GetTicketsAsync(PagedRequestDto req, CancellationToken ct = default)
        => throw new NotImplementedException();
}
