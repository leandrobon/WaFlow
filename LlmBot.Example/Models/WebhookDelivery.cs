using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LlmBot.Example.Models;

public sealed class WebhookDelivery
{
    [JsonPropertyName("waFlowVersion")]
    public string WaFlowVersion { get; init; } = "v0";

    [Required, MinLength(1)]
    [JsonPropertyName("messages")]
    public required List<WebhookDeliveryMessage> Messages { get; init; }

    [JsonPropertyName("signature")]
    public string? Signature { get; init; }
}

public sealed class WebhookDeliveryMessage
{
    [Required, JsonPropertyName("from")]
    public required string From { get; init; }

    [Required, JsonPropertyName("type")]
    public required MessageType Type { get; init; }

    [JsonPropertyName("text")]
    public TextBody? Text { get; init; }

    [Required, JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }
}