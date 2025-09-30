using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebApplication1.Hubs;
using WebApplication1.Models;
using WebApplication1.Models.Enums;
using WebApplication1.Services;
using WebApplication1.Services.InMemory;
using WebApplication1.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IRegistry, InMemoryRegistry>();
builder.Services.AddSingleton<IMessageStore>(sp => new InMemoryMessageStore(maxMessages: 5000));
// HttpClient factory for dispatcher
builder.Services.AddHttpClient();
// Dispatcher 
builder.Services.AddSingleton<IWebhookDispatcher, WebhookDispatcher>();

builder.Services.ConfigureHttpJsonOptions(opt =>
{
    opt.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opt.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(o =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                  ?? Array.Empty<string>();

    o.AddPolicy("ui-dev", p => p
        .WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHub<ChatHub>("/stream"); // Real time
app.MapGet("/healthz", () => Results.Ok("ok"));

//Endpoints

app.MapPost("/webhook/register", (WebhookRegistration reg, IRegistry registry) =>
    {
        registry.Set(reg);
        return Results.Ok(new { ok = true });
    })
    .Produces(StatusCodes.Status200OK);

app.MapPost("/simulate/user",
    async (SimulateUserInput input,
           IRegistry registry,
           IMessageStore store,
           IWebhookDispatcher dispatcher,
           IHubContext<ChatHub> hub,
           ILoggerFactory lf,
           CancellationToken ct) =>
{
    var logger = lf.CreateLogger("SimulateUser");

    // Build inbound message
    var msg = new Message
    {
        Id = Guid.NewGuid().ToString(),
        Direction = Direction.Inbound,
        Type = MessageType.Text,
        From = input.UserId,
        To = "bot",
        Text = new TextBody { Body = input.Text },
        Timestamp = DateTimeOffset.UtcNow
    };

    store.Add(msg);

    // Emit in real time for ui(not in use yet)
    await hub.Clients.All.SendAsync("message", msg, ct);

    // Send to webhook if registered
    var reg = registry.Get();
    if (reg is null)
    {
        logger.LogInformation("No webhook registered. Not delivered");
        return Results.Accepted(value: new { delivered = false, reason = "no_webhook_registered" });
    }

    var delivery = new WebhookDelivery
    {
        Messages = new List<WebhookDeliveryMessage>
        {
            new()
            {
                From = msg.From,
                Type = msg.Type,
                Text = msg.Text,
                Timestamp = msg.Timestamp
            }
        }
    };

    var result = await dispatcher.DeliverAsync(reg, delivery, ct);

    return Results.Accepted(value: new
    {
        delivered = result.Success,
        statusCode = result.StatusCode,
        error = result.Error
    });
})
.Produces(StatusCodes.Status202Accepted);

// 3) Outbound message from bot to user
app.MapPost("/messages",
    async (BotSendText input,
           IMessageStore store,
           IHubContext<ChatHub> hub,
           CancellationToken ct) =>
{
    var msg = new Message
    {
        Id = Guid.NewGuid().ToString(),
        Direction = Direction.Outbound,
        Type = MessageType.Text,
        From = "bot",
        To = input.To,
        Text = new TextBody { Body = input.Text },
        Timestamp = DateTimeOffset.UtcNow
    };

    store.Add(msg);
    await hub.Clients.All.SendAsync("message", msg, ct);

    return Results.Ok(msg);
})
.Produces<Message>(StatusCodes.Status200OK);

// --- READ/RESET utils ---

// List messages userId=user-001)
app.MapGet("/messages",
        ([FromServices] IMessageStore store,
            [FromQuery] string? userId,
            [FromQuery] long? sinceSeq) =>
        {
            if (sinceSeq.HasValue && !string.IsNullOrWhiteSpace(userId))
            {
                var msgs = store.GetSince(userId!, sinceSeq.Value);
                return Results.Ok(msgs);
            }

            if (sinceSeq.HasValue)
            {
                var msgs = store.GetSince(sinceSeq.Value);
                return Results.Ok(msgs);
            }

            // if no seq num given return all
            var all = string.IsNullOrWhiteSpace(userId) ? store.GetAll() : store.GetByUser(userId!);
            return Results.Ok(all);
        })
    .Produces<IEnumerable<Message>>(StatusCodes.Status200OK);

app.MapDelete("/messages", (IMessageStore store) =>
    {
        store.Clear();
        return Results.NoContent();
    })
    .Produces(StatusCodes.Status204NoContent);

// See registered webhook
app.MapGet("/webhook", (IRegistry registry) =>
    {
        var reg = registry.Get();
        return reg is null ? Results.NoContent() : Results.Ok(reg);
    })
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status204NoContent);

// Delete webhook
app.MapDelete("/webhook", (IRegistry registry) =>
    {
        registry.Clear();
        return Results.NoContent();
    })
    .Produces(StatusCodes.Status204NoContent);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("ui-dev");
}

//app.UseHttpsRedirection();

app.Run();

