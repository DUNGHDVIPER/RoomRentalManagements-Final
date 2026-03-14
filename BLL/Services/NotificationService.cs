using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Notification;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.System;
using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    // ===============================
    // GỬI THÔNG BÁO
    // ===============================
    public async Task BroadcastAsync(
     BroadcastNotificationDto dto,
     CancellationToken ct = default)
    {
        var contractIds = new List<int>();

        // =========================
        // LẤY CONTRACT ID ĐÚNG
        // =========================
        if (dto.ContractIds != null && dto.ContractIds.Any())
        {
            contractIds = await _context.Contracts
                .Where(c => dto.ContractIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(ct);
        }
        else if (dto.BlockId.HasValue)
        {
            contractIds = await _context.Contracts
                .Include(c => c.Room)
                    .ThenInclude(r => r.Floor)
                .Where(c => c.Room.Floor.BlockId == dto.BlockId.Value)
                .Select(c => c.Id)
                .ToListAsync(ct);
        }
        else if (dto.FloorId.HasValue)
        {
            contractIds = await _context.Contracts
                .Where(c => c.Room.FloorId == dto.FloorId.Value)
                .Select(c => c.Id)
                .ToListAsync(ct);
        }

        var now = DateTime.UtcNow;
        var notifications = new List<Notification>();

        // ===== TENANT =====
        foreach (var contractId in contractIds)
        {
            notifications.Add(new Notification
            {
                ContractId = contractId,
                Title = dto.Title,
                Content = dto.Content,
                IsRead = false,
                CreatedAt = now,
                SourceType = dto.SourceType
            });
        }

        // ===== HOST =====
        if (dto.SendToHost)
        {
            var hostRoleId = await _context.Roles
                .Where(r => r.Name == "Host")
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrEmpty(hostRoleId))
            {
                var hostIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == hostRoleId)
                    .Select(ur => ur.UserId)
                    .ToListAsync(ct);

                foreach (var hostId in hostIds)
                {
                    notifications.Add(new Notification
                    {
                        ReceiverUserId = hostId,
                        Title = dto.Title,
                        Content = dto.Content,
                        IsRead = false,
                        CreatedAt = now,
                        SourceType = dto.SourceType
                    });
                }
            }
        }

        if (!notifications.Any())
            return;

        await _context.Notifications.AddRangeAsync(notifications, ct);
        await _context.SaveChangesAsync(ct);
    }

    // ===============================
    // LẤY DANH SÁCH CỦA TENANT
    // ===============================
    public async Task<PagedResultDto<NotificationDto>> GetUserNotificationsAsync(
        int contractId,
        PagedRequestDto request,
        CancellationToken ct = default)
    {
        var query = _context.Notifications
            .Where(x => x.ContractId ==  contractId)
            .OrderByDescending(x => x.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Title = x.Title,
                Content = x.Content,
                CreatedAt = x.CreatedAt,
                IsRead = x.IsRead
            })
            .ToListAsync(ct);

        return new PagedResultDto<NotificationDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    // ===============================
    // MARK AS READ
    // ===============================
    public async Task MarkReadAsync(
      int notificationId,
    int? contractId,
    string? userId,
    CancellationToken ct = default)
    {
        var entity = await _context.Notifications
            .FirstOrDefaultAsync(x =>
                x.Id == notificationId &&
                (
                    (contractId.HasValue && x.ContractId == contractId) ||
                    (!string.IsNullOrEmpty(userId) && x.ReceiverUserId == userId)
                ),
                ct);

        if (entity == null)
            return;

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            await _context.SaveChangesAsync(ct);
        }
    }

    // ===============================
    // ĐẾM CHƯA ĐỌC
    // ===============================
    public async Task<int> GetUnreadCountAsync(
        int tenantId,
        CancellationToken ct = default)
    {
        return await _context.Notifications
            .CountAsync(x => x.ContractId == tenantId && !x.IsRead, ct);
    }

    public async Task<List<NotificationDto>> GetHostNotificationsAsync(
    string userId,
    CancellationToken ct = default)
    {
        return await _context.Notifications
            .Where(x => x.ReceiverUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Title = x.Title,
                Content = x.Content,
                CreatedAt = x.CreatedAt,
                IsRead = x.IsRead
            })
            .ToListAsync(ct);
    }

    public async Task<int?> GetActiveContractIdByUserIdAsync(string userId)
    {
        return await _context.Contracts
            .Where(c => c.Tenant.UserId == userId && c.Status == "Active")
            .Select(c => (int?)c.ContractId)
            .FirstOrDefaultAsync();
    }
}