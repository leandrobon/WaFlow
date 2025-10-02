using Microsoft.AspNetCore.SignalR;

namespace WebApplication1.Hubs;

public sealed class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }

    public Task Join(string userId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, userId);
}