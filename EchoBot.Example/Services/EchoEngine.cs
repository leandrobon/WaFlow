using EchoBot.Example.Models;

namespace EchoBot.Example.Services;

public sealed class EchoEngine : IBotEngine
{
    public Task<string?> ReplyAsync(WebhookDeliveryMessage msg, CancellationToken ct = default)
    {
        if (msg.Type != MessageType.Text || msg.Text is null) return Task.FromResult<string?>(null);
        return Task.FromResult<string?>($"Echo: {msg.Text.Body}");
    }
}