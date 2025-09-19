using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAFlow.Chat.Components;
using WAFlow.Chat.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SimulatorOptions>(
    builder.Configuration.GetSection("Simulator"));

builder.Services.AddHttpClient("sim", (sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<SimulatorOptions>>().Value;

    // Normalize to avoid "http://...//api"
    var baseUrl = opt.BaseUrl?.TrimEnd('/') ?? "http://localhost:5080";
    http.BaseAddress = new Uri(baseUrl + "/");
    http.Timeout = TimeSpan.FromSeconds(10);
});


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<IChatBackend, SimulatorHttpBackend>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

//export messages as json
app.MapGet("/export", async ([FromQuery] string? userId, IHttpClientFactory httpFactory) =>
{
    var uid = string.IsNullOrWhiteSpace(userId) ? "user-001" : userId.Trim();
    var client = httpFactory.CreateClient("sim");
    
    var json = await client.GetStringAsync($"/messages?userId={Uri.EscapeDataString(uid)}");
    var bytes = Encoding.UTF8.GetBytes(json);

    var fileName = $"session-{uid}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
    return Results.File(bytes, "application/json", fileName);
});

app.Run();