using System.Text.Json.Serialization;

namespace WebApplication1.Models;

public sealed class ErrorResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("details")]
    public object? Details { get; init; }
}