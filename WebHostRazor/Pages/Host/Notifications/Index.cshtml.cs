using BLL.DTOs.Notification;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebHostRazor.Pages.Host.Notifications;

public class IndexModel : PageModel
{
    private readonly INotificationService _notificationService;

    public List<NotificationDto> Notifications { get; set; } = new();

    public IndexModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            Notifications = await _notificationService
                .GetHostNotificationsAsync(userId);
        }
    }

    public async Task<IActionResult> OnPostMarkRead(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _notificationService
            .MarkReadAsync((int)id, null, userId);

        return RedirectToPage();
    }
}