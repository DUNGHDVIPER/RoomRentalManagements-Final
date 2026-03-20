using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Ticket;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.Common;
using DAL.Entities.Maintenance;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class TicketService : ITicketService
{
    private readonly AppDbContext _context;

    public TicketService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int?> GetTenantIdByEmail(string email)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Email == email);

        Console.WriteLine($"Find Tenant by Email: {email}");
        Console.WriteLine($"Result: {(tenant == null ? "NULL" : tenant.Id.ToString())}");

        return tenant?.Id;
    }

    public async Task<bool> CreateAsync(CreateTicketDto dto, string email)
    {
        Console.WriteLine("===== CREATE TICKET =====");

        // 🔥 tìm tenant bằng email
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Email == email);

        if (tenant == null)
        {
            Console.WriteLine("❌ Tenant not found");
            return false;
        }

        // 🔥 check room
        var room = await _context.Rooms.FindAsync(dto.RoomId);
        if (room == null)
        {
            Console.WriteLine("❌ Room not found");
            return false;
        }

        var ticket = new Ticket
        {
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            RoomId = dto.RoomId,
            TenantId = tenant.Id, // ✅ set ở đây
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        Console.WriteLine("✅ CREATED");

        return true;
    }

    public Task<TicketDto> AssignAsync(AssignTicketDto dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TicketDto> UpdateStatusAsync(UpdateTicketStatusDto dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<PagedResultDto<TicketDto>> GetTicketsAsync(PagedRequestDto req, CancellationToken ct = default)
        => throw new NotImplementedException();
}