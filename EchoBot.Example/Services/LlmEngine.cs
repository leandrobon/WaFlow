using EchoBot.Example.Models;

namespace EchoBot.Example.Services;

public sealed class LlmEngine : IBotEngine
{
    private readonly GeminiClient _gemini;
    private readonly IConfiguration _cfg;

    public LlmEngine(GeminiClient gemini, IConfiguration cfg)
    {
        _gemini = gemini;
        _cfg = cfg;
    }

    public async Task<string?> ReplyAsync(WebhookDeliveryMessage msg, CancellationToken ct)
    {
        if (msg.Text?.Body is null) return null;

        var systemPrompt = _cfg["WAFlowBot:Gemini:SystemPrompt"]
                           ?? "You are a concise, helpful WhatsApp-style assistant.";

        return await _gemini.GenerateAsync(systemPrompt, msg.Text.Body, ct);
    }
}