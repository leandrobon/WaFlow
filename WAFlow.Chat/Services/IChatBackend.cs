using WAFlow.Chat.Models;

namespace WAFlow.Chat.Services;

public interface IChatBackend
{
    IReadOnlyList<Message> Feed { get; }
    event Action<Message>? OnMessage;

    Task InitializeAsync(CancellationToken ct = default); // Initial load + opens the SignalR connection
    Task SendFromUserAsync(string text, CancellationToken ct = default);
    Task ResetAsync(CancellationToken ct = default);
    bool IsOnline { get; }
    event Action<bool>? OnlineChanged;
}