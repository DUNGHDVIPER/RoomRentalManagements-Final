using Microsoft.AspNetCore.SignalR;

namespace WebHostRazor.Hubs;

public class NotificationHub : Hub
{
    public async Task JoinTenantGroup(int tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Tenant_{tenantId}");
    }
}