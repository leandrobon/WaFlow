namespace WAFlow.Chat.Models;

public enum Direction { Inbound, Outbound }

public sealed class Message
{
    public Direction Direction { get; init; }
    public string Body { get; init; } = "";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static Message User(string body) => new()
    {
        Direction = Direction.Inbound, Body = body, Timestamp = DateTime.UtcNow
    };

    public static Message Bot(string body) => new()
    {
        Direction = Direction.Outbound, Body = body, Timestamp = DateTime.UtcNow
    };
}
