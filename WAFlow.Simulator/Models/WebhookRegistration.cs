using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApplication1.Models;

public sealed class WebhookRegistration
{
    [Required, StringLength(64, MinimumLength = 1), JsonPropertyName("botId")]
    public required string BotId { get; init; }

    [Required, Url, StringLength(2048), JsonPropertyName("webhookUrl")]
    public required string WebhookUrl { get; init; }

    // Recommended to verify signature or basic auth
    [StringLength(64, MinimumLength = 16), JsonPropertyName("secret")]
    public string? Secret { get; init; }

}