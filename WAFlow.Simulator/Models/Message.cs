using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WebApplication1.Models.Enums;

namespace WebApplication1.Models;

public sealed class Message
{
    [Required, JsonPropertyName("id")] public required string Id { get; init; }

    [Required, JsonPropertyName("direction")]
    public required Direction Direction { get; init; }

    [Required, JsonPropertyName("type")] public required MessageType Type { get; init; }

    [Required, StringLength(128, MinimumLength = 1), JsonPropertyName("from")]
    public required string From { get; init; }

    [Required, StringLength(128, MinimumLength = 1), JsonPropertyName("to")]
    public required string To { get; init; }

    // Required if Type==Text, validate on endpoint
    [JsonPropertyName("text")] public TextBody? Text { get; init; }

    [Required, JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }
    //Use it to identify message sequences in CLI replays.
    public long Seq { get; set; }
}