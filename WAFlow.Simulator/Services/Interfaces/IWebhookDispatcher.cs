using WebApplication1.Models;

namespace WebApplication1.Services.Interfaces;

public interface IWebhookDispatcher
{
    Task<DispatchResult> DeliverAsync(
        WebhookRegistration registration,
        WebhookDelivery delivery,
        CancellationToken ct = default);
}

public sealed record DispatchResult(
    bool Success,
    int? StatusCode,
    string? Error);