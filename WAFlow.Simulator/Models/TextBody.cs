using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApplication1.Models;

public sealed class TextBody
{
    [Required]
    [StringLength(4096, MinimumLength = 1)]
    [JsonPropertyName("body")]
    public required string Body { get; init; }
}