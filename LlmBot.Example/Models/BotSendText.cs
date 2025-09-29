using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LlmBot.Example.Models;

public sealed class BotSendText
{
    [Required, JsonPropertyName("to")]
    public required string To { get; init; }

    [Required, JsonPropertyName("text")]
    public required string Text { get; init; }
}