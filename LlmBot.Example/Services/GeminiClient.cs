using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlmBot.Example.Services;

public sealed class GeminiClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiClient(IHttpClientFactory factory, IConfiguration cfg)
    {
        _http = factory.CreateClient(nameof(GeminiClient));
        _apiKey = cfg["WAFlowBot:Gemini:ApiKey"]
                  ?? throw new InvalidOperationException("Missing WAFlowBot:Gemini:ApiKey");
        _model = cfg["WAFlowBot:Gemini:Model"] ?? "gemini-1.5-flash-latest";
    }

    private sealed class Part
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
    }

    private sealed class Content
    {
        [JsonPropertyName("role")] public string? Role { get; set; }
        [JsonPropertyName("parts")] public Part[]? Parts { get; set; }
    }
    
    private sealed class GenerateReq
    {
        [JsonPropertyName("contents")] public Content[]? Contents { get; set; }

        [JsonPropertyName("systemInstruction")]
        public Content? SystemInstruction { get; set; }
    }

    public async Task<string?> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";

        var reqBody = new GenerateReq
        {
            SystemInstruction = new Content { Role = "system", Parts = new[] { new Part { Text = systemPrompt } } },
            Contents = new[]
            {
                new Content { Role = "user", Parts = new[] { new Part { Text = userMessage } } }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(reqBody)
        };
        // Move key to header → no key in logs
        req.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);

        using var resp = await _http.SendAsync(req, ct);
        var payload = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini API error {(int)resp.StatusCode}: {payload}");
        }

        using var doc = JsonDocument.Parse(payload);
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0) return null;

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content)) return null;
        if (!content.TryGetProperty("parts", out var parts)) return null;
        if (parts.GetArrayLength() == 0) return null;

        var text = parts[0].GetProperty("text").GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}