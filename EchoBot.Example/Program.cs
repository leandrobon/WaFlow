using System.Text.Json;
using System.Text.Json.Serialization;
using EchoBot.Example.Models;
using EchoBot.Example.Options;
using EchoBot.Example.Services;

var builder = WebApplication.CreateBuilder(args);

// JSON global
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Options + HttpClient
builder.Services.Configure<BotOptions>(builder.Configuration.GetSection("WAFlowBot"));
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IWhatsAppClient, SimulatorClient>();
builder.Services.AddSingleton<IBotEngine, EchoEngine>(); // to use ai, change to llmEngine
builder.Services.AddHostedService<WebhookRegisterHostedService>();

var app = builder.Build();

// Webhook 
app.MapPost("/bot/webhook",
    async (WebhookDelivery delivery, IWhatsAppClient wa, IBotEngine engine, IConfiguration cfg, HttpRequest req, CancellationToken ct) =>
    {
        // 
        var expected = cfg["WAFlowBot:Secret"];
        if (!string.IsNullOrEmpty(expected))
        {
            req.Headers.TryGetValue("X-WAFlow-Secret", out var got);
            if (got.Count == 0 || got[0] != expected)
            {
                // still v0, but we register
                app.Logger.LogWarning("X-WAFlow-Secret inválido o ausente");
            }
        }

        int sent = 0;
        foreach (var msg in delivery.Messages)
        {
            var reply = await engine.ReplyAsync(msg, ct);
            if (!string.IsNullOrWhiteSpace(reply))
            {
                await wa.SendTextAsync(msg.From, reply!, ct);
                sent++;
            }
        }
        return Results.Ok(new { received = delivery.Messages.Count, sent });
    });

app.MapGet("/", () => "EchoBot up");
app.Run();