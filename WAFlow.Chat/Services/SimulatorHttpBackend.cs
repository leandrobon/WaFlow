using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WAFlow.Chat.Models;

namespace WAFlow.Chat.Services;

// === DTOs del simulator ===
internal sealed class TextBodyDto { [JsonPropertyName("body")] public string? Body { get; set; } }

internal sealed class SimMessageDto
{
    [JsonPropertyName("id")]        public string? Id { get; set; }
    [JsonPropertyName("direction")] public string? Direction { get; set; } 
    [JsonPropertyName("type")]      public string? Type { get; set; }      // "Text"...
    [JsonPropertyName("from")]      public string? From { get; set; }
    [JsonPropertyName("to")]        public string? To { get; set; }
    [JsonPropertyName("text")]      public TextBodyDto? Text { get; set; }
    [JsonPropertyName("timestamp")] public DateTimeOffset Timestamp { get; set; }
}
file sealed class SimulateUserInputDto
{
    [JsonPropertyName("userId")] public string UserId { get; set; } = "user-001";
    [JsonPropertyName("text")]   public string Text   { get; set; } = "";
}

public sealed class SimulatorHttpBackend : IChatBackend, IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;
    private readonly List<Message> _feed = new();
    private Timer? _timer;
    private int _lastCount = 0;
    private bool _isOnline;

    // one unique simulated user for now
    private readonly string _userId = "user-001";

    public SimulatorHttpBackend(IHttpClientFactory f)
    {
        _http = f.CreateClient("sim");
        _json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public IReadOnlyList<Message> Feed => _feed;
    public event Action<Message>? OnMessage;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<SimMessageDto>>($"/messages?userId={_userId}", _json, ct)
                       ?? new List<SimMessageDto>();

            _feed.Clear();
            foreach (var d in list) _feed.Add(Map(d));
            _lastCount = _feed.Count;

            IsOnline = true; 
        }
        catch
        {
            IsOnline = false; 
        }

        // light polling (later migrate to SignalR /stream)
        _timer ??= new Timer(async _ => await Poll(), null, 400, 400);
    }

    public async Task SendFromUserAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!IsOnline) throw new InvalidOperationException("Simulator offline.");

        var payload = new SimulateUserInputDto { UserId = _userId, Text = text };
        var resp = await _http.PostAsJsonAsync("/simulate/user", payload, _json, ct);
        resp.EnsureSuccessStatusCode();
    }

    private async Task Poll()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<SimMessageDto>>($"/messages?userId={_userId}", _json)
                       ?? new List<SimMessageDto>();

            if (!IsOnline) IsOnline = true;

            if (list.Count > _lastCount)
            {
                foreach (var d in list.Skip(_lastCount))
                {
                    var m = Map(d);
                    _feed.Add(m);
                    OnMessage?.Invoke(m);
                }

                _lastCount = list.Count;
            }
        }
        catch
        {
            if (IsOnline) IsOnline = false;
        }
    }
    
    public async Task ResetAsync(CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/messages", ct);
        resp.EnsureSuccessStatusCode();

        // clean local state so nothing reappears
        _feed.Clear();
        _lastCount = 0;
    }

    private static Message Map(SimMessageDto d) => new()
    {
        Direction = string.Equals(d.Direction, "Outbound", StringComparison.OrdinalIgnoreCase)
                    ? Direction.Outbound : Direction.Inbound,
        Body = d.Text?.Body ?? "",
        Timestamp = d.Timestamp.UtcDateTime
    };

    public ValueTask DisposeAsync()
    {
        _timer?.Dispose();
        return ValueTask.CompletedTask;
    }
    
    public bool IsOnline
    {
        get => _isOnline;
        private set
        {
            if (_isOnline != value)
            {
                _isOnline = value;
                OnlineChanged?.Invoke(value);
            }
        }
    }

    public event Action<bool>? OnlineChanged;
}
