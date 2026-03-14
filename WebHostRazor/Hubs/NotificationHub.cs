using Microsoft.AspNetCore.SignalR;

namespace WebHostRazor.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var contractId = Context.GetHttpContext()
                ?.Request.Query["contractId"];

            if (!string.IsNullOrEmpty(contractId))
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    $"Contract_{contractId}");
            }

            await base.OnConnectedAsync();
        }
    }
}
