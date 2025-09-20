using System.Net.Mime;
using System.Text;
using System.Text.Json;
using EchoBot.Example.Options;
using Microsoft.Extensions.Options;

namespace EchoBot.Example.Services;

public sealed class WebhookRegisterHostedService : IHostedService
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<WebhookRegisterHostedService> _logger;
    private readonly BotOptions _opts;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public WebhookRegisterHostedService(
        IHttpClientFactory factory,
        IOptions<BotOptions> opts,
        ILogger<WebhookRegisterHostedService> logger)
    {
        _factory = factory;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var client = _factory.CreateClient("waflow-simulator");
            var url = _opts.SimulatorBaseUrl.TrimEnd('/') + "/webhook/register";

            var payload = new
            {
                botId = _opts.BotId,
                webhookUrl = _opts.MyWebhookUrl,
                secret = _opts.Secret
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload, Json);
            using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            using var resp = await client.PostAsync(url, content, ct);

            if (resp.IsSuccessStatusCode)
                _logger.LogInformation("Webhook registered OK ({Status}) on {Url}", (int)resp.StatusCode, url);
            else
                _logger.LogWarning("Webhook registered FAIL ({Status}) on {Url}", (int)resp.StatusCode, url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhook");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
