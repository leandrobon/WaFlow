using LlmBot.Example.Models;

namespace LlmBot.Example.Services;

public interface IBotEngine
{
    Task<string?> ReplyAsync(WebhookDeliveryMessage msg, CancellationToken ct = default);

}