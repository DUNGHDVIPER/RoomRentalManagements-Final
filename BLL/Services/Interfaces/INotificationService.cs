using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Notification;

namespace BLL.Services.Interfaces;


public interface INotificationService
{
    Task BroadcastAsync(
        BroadcastNotificationDto dto,
        CancellationToken ct = default);

    Task<PagedResultDto<NotificationDto>> GetUserNotificationsAsync(
        int contractId,
        PagedRequestDto request,
        CancellationToken ct = default);

    Task<List<NotificationDto>> GetHostNotificationsAsync(
        string userId,
        CancellationToken ct = default);

    Task MarkReadAsync(
        int notificationId,
        int? contractId,
        string? userId,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(
        int contractId,
        CancellationToken ct = default);
}