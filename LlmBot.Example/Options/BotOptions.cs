namespace LlmBot.Example.Options;

public sealed class BotOptions
{
    public string SimulatorBaseUrl { get; init; } = "http://localhost:5080";
    public string MyWebhookUrl     { get; init; } = "http://localhost:5199/bot/webhook";
    public string? Secret          { get; init; } = "dev";
    public string BotId            { get; init; } = "echo";
}