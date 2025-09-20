using System.Diagnostics;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using WebApplication1.Models;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Services;

public sealed class WebhookDispatcher: IWebhookDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WebhookDispatcher> _logger;
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        public WebhookDispatcher(IHttpClientFactory httpClientFactory, ILogger<WebhookDispatcher> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<DispatchResult> DeliverAsync(
            WebhookRegistration registration,
            WebhookDelivery delivery,
            CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient("waflow-webhook");

            var json = JsonSerializer.Serialize(delivery, JsonOpts);
            using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            using var req = new HttpRequestMessage(HttpMethod.Post, registration.WebhookUrl)
            {
                Content = content
            };

            // Simple auth header. Later maybe HMAC signature
            if (!string.IsNullOrWhiteSpace(registration.Secret))
            {
                req.Headers.Add("X-WAFlow-Secret", registration.Secret);
            }

            var sw = Stopwatch.StartNew();
            try
            {
                using var resp = await client.SendAsync(req, ct);
                sw.Stop();

                if (resp.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Webhook OK {Status} {Url} ({Elapsed} ms)",
                        (int)resp.StatusCode, registration.WebhookUrl, sw.ElapsedMilliseconds);

                    return new DispatchResult(true, (int)resp.StatusCode, null);
                }

                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Webhook FAIL {Status} {Url} ({Elapsed} ms) Body={Body}",
                    (int)resp.StatusCode, registration.WebhookUrl, sw.ElapsedMilliseconds, Truncate(body, 500));

                return new DispatchResult(false, (int)resp.StatusCode, body);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogWarning("Webhook CANCELLED {Url} ({Elapsed} ms)", registration.WebhookUrl, sw.ElapsedMilliseconds);
                return new DispatchResult(false, null, "cancelled");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Webhook ERROR {Url} ({Elapsed} ms)", registration.WebhookUrl, sw.ElapsedMilliseconds);
                return new DispatchResult(false, null, ex.Message);
            }
        }

        private static string Truncate(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
}