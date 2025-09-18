using System.Text.Json;
using System.Text.Json.Serialization;
using WAFlow.Chat.Models;

namespace WAFlow.Chat.Services;

public sealed class ReplayOptions
{
    public bool GateOnReply { get; init; } = true;          // wait for bot response
    public int ReplyTimeoutMs { get; init; } = 20000;       // 20s
    public bool RespectUserGaps { get; init; } = false;     // (dosent work)respect original gaps between user msg
    public double Speed { get; init; } = 1.0;               // 2.0 = 2x faster (if respectusergaps)
}

internal sealed class ExportItem
{
    public string? Direction { get; set; }   
    public string? Body { get; set; }
    public DateTime Timestamp { get; set; }
}

internal sealed class TextPayload
{
    public string? Body { get; set; }
}

internal sealed class UploadedMessage
{
    public string? Direction { get; set; }
    public TextPayload? Text { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed class ReplayMessages : IAsyncDisposable
{
    private readonly IChatBackend _chat;
    private CancellationTokenSource? _cts;
    private bool _running;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ReplayMessages(IChatBackend chat) => _chat = chat;

    public bool IsRunning => _running;

    public async Task StartFromStreamAsync(Stream fileStream, ReplayOptions opt)
    {
        var exported = await ParseExportAsync(fileStream);
        await StartFromMessagesAsync(exported, opt);
    }

    public async Task StartFromMessagesAsync(List<Message> exported, ReplayOptions opt)
    {
        if (_running) throw new InvalidOperationException("There is already an active replay.");
        _cts = new CancellationTokenSource();
        _running = true;

        try
        {
            var userMsgs = exported
                .Where(m => m.Direction == Direction.Inbound && !string.IsNullOrWhiteSpace(m.Body))
                .OrderBy(m => m.Timestamp)
                .ToList();

            if (userMsgs.Count == 0)
                throw new InvalidDataException("El JSON no contiene mensajes de usuario (Inbound) con texto.");

            for (int i = 0; i < userMsgs.Count; i++)
            {
                _cts.Token.ThrowIfCancellationRequested();

                //respect user gaps
                if (opt.RespectUserGaps && i > 0)
                {
                    var gap = userMsgs[i].Timestamp - userMsgs[i - 1].Timestamp;
                    if (gap < TimeSpan.Zero) gap = TimeSpan.Zero;
                    if (opt.Speed > 0) gap = TimeSpan.FromMilliseconds(gap.TotalMilliseconds / opt.Speed);
                    if (gap > TimeSpan.Zero) await Task.Delay(gap, _cts.Token);
                }

                // prepare wait for answer before injecting
                Task<bool> waitBot = Task.FromResult(true);
                if (opt.GateOnReply)
                    waitBot = WaitForBotAsync(TimeSpan.FromMilliseconds(opt.ReplyTimeoutMs), _cts.Token);
                
                await _chat.SendFromUserAsync(userMsgs[i].Body, _cts.Token);
                
                if (opt.GateOnReply)
                    await waitBot;
            }
        }
        finally
        {
            _running = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    private async Task<bool> WaitForBotAsync(TimeSpan timeout, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedAt = DateTime.UtcNow;

        void Handler(Message m)
        {
            // ignore inbound, complete on first outbound
            if (m.Direction == Direction.Outbound && m.Timestamp.ToUniversalTime() >= startedAt)
            {
                _chat.OnMessage -= Handler;
                tcs.TrySetResult(true);
            }
        }

        _chat.OnMessage += Handler;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var delayTask = Task.Delay(timeout, cts.Token);
            var finished = await Task.WhenAny(tcs.Task, delayTask);
            if (finished == tcs.Task) { cts.Cancel(); return true; }
            // timeout: unsuscribe and keep going
            return false;
        }
        finally
        {
            _chat.OnMessage -= Handler;
        }
    }

    private static async Task<List<Message>> ParseExportAsync(Stream s)
    {
        using var sr = new StreamReader(s);
        var json = await sr.ReadToEndAsync();
        
        var uploadedMessages = JsonSerializer.Deserialize<List<UploadedMessage>>(json, _json);

        if (uploadedMessages is null || uploadedMessages.Count == 0)
        {
            throw new InvalidDataException("JSON is empty or not in the expected format.");
        }
        
        return uploadedMessages
            .Select(m => new Message
            {
                Direction = Enum.TryParse<Direction>(m.Direction, true, out var d) ? d : Direction.Inbound,
                Body = m.Text?.Body ?? "",
                Timestamp = m.Timestamp
            })
            .ToList();
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        return ValueTask.CompletedTask;
    }
}
