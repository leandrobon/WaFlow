namespace WAFlow.Chat.Services;

public sealed class SimulatorOptions
{
    public string BaseUrl { get; init; } = "http://localhost:5080";
    public string HealthPath { get; init; } = "/health";
    public string SendPath { get; init; } = "/api/messages";
    public int HeartbeatSeconds { get; init; } = 5;
}