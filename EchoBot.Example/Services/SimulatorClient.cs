using System.Net.Mime;
using System.Text;
using System.Text.Json;
using EchoBot.Example.Models;
using EchoBot.Example.Options;
using Microsoft.Extensions.Options;

namespace EchoBot.Example.Services;

public sealed class SimulatorClient : IWhatsAppClient
{
    private readonly IHttpClientFactory _factory;
    private readonly BotOptions _opts;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SimulatorClient(IHttpClientFactory factory, IOptions<BotOptions> opts)
    {
        _factory = factory;
        _opts = opts.Value;
    }

    public async Task SendTextAsync(string to, string text, CancellationToken ct = default)
    {
        var client = _factory.CreateClient("waflow-simulator");
        var url = _opts.SimulatorBaseUrl.TrimEnd('/') + "/messages";

        var payload = new BotSendText { To = to, Text = text };
        var json = System.Text.Json.JsonSerializer.Serialize(payload, Json);
        using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        using var resp = await client.PostAsync(url, content, ct);
        resp.EnsureSuccessStatusCode();
    }
}