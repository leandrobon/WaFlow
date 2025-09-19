using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApplication1.Models;

public class SimulateUserInput
{
    [Required, StringLength(128, MinimumLength = 1), JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [Required, StringLength(4096, MinimumLength = 1), JsonPropertyName("text")]
    public required string Text { get; init; }
}