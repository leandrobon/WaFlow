using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApplication1.Models;

public sealed class BotSendText
{
    [Required, StringLength(128, MinimumLength = 1), JsonPropertyName("to")]
    public required string To { get; init; }

    [Required, StringLength(4096, MinimumLength = 1), JsonPropertyName("text")]
    public required string Text { get; init; }
}