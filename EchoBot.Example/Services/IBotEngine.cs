using EchoBot.Example.Models;

namespace EchoBot.Example.Services;

public interface IBotEngine
{
    Task<string?> ReplyAsync(WebhookDeliveryMessage msg, CancellationToken ct = default);

}