using Microsoft.AspNetCore.SignalR;

namespace WebApplication1.Hubs;

/// <summary>
/// Real time channel. Doenst expose methods
/// used to emmit "messages" from endpoints.
/// </summary>

public sealed class ChatHub : Hub
{
    
}