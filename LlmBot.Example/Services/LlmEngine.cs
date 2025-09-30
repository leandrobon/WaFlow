using LlmBot.Example.Models;

namespace LlmBot.Example.Services;

public sealed class LlmEngine : IBotEngine
{
    private readonly GeminiClient _gemini;
    private readonly IConfiguration _cfg;
    private readonly string _waFlowContext = """
WAFlow (MVP) — Condensed Context

What it is
- A minimal local sandbox to build/test webhook-based chatbots end-to-end.
- Browser chat UI → Simulator posts your webhook → your bot replies via a simple API.
- Conversation replay: export/import JSON transcripts for quick regression tests.

Architecture
- WAFlow.Simulator (.NET Minimal API): inject user messages, dispatch webhooks, store feed.
- WAFlow.Chat (Blazor UI): polling chat, export/import, reset, online banner.
- LlmBot.Example: sample bot using Gemini.
- docker-compose orchestrates all.

Ports (host)
- UI: 5056
- Simulator: 5080
- LlmBot: 5199
In Docker, services talk on port 8080 via service DNS:
- http://sim:8080  (simulator)
- http://llmbot:8080 (example bot)

Requirements
- Docker Desktop
- Optional: .NET 8 SDK (run outside Docker)
- Optional: GOOGLE_API_KEY (only for the example LlmBot)

Quickstart
1) git clone <repo> waflow && cd waflow
2) echo "GOOGLE_API_KEY=your_gemini_key" > .env   (only if using LlmBot.Example)
3) docker compose up -d --build
Open http://localhost:5056 to chat.
Tail logs: 
- docker compose logs -f sim
- docker compose logs -f llmbot

Webhook hookup
A) Auto (Docker): llmbot self-registers using:
   WAFlowBot__SimulatorBaseUrl=http://sim:8080
   WAFlowBot__MyWebhookUrl=http://llmbot:8080/bot/webhook
B) Manual (any env):
   POST http://localhost:5080/webhook/register 
   body: {"url":"http://localhost:5199/bot/webhook"}
Check stored webhook: GET http://localhost:5080/webhook
Delete: DELETE http://localhost:5080/webhook

Replay transcripts
- In the UI, click Import and choose a previously exported .json.
- Export from the UI to create test fixtures for regression.

Limitations (MVP)
- Polling UI (no SignalR push yet).
- Single user feed in UI.
- Text messages only (models ready for richer types, not exposed).
- Single global webhook, no auth.
- Export/Import schema v0 (may change before v0.1).

Short roadmap
1) SignalR real-time feed/events.
2) User selector (per-user feeds).
3) Rich message types (image/buttons/lists) + basic rendering.
4) CLI replay runner (batch transcripts + compare).
5) Per-user webhooks, HMAC signing, retries.
6) UI logs panel (deliveries/retries/errors).
7) Integration tests & fixtures.

Troubleshooting
- UI OFFLINE → check `docker compose logs -f sim` and base URLs 
  (Docker: http://sim:8080; Local: http://localhost:5080).
- Webhook not receiving → verify GET /webhook or re-register.
- Inter-service calls in Docker → never use localhost; use sim:8080 and llmbot:8080.

License: MIT
""";


    public LlmEngine(GeminiClient gemini, IConfiguration cfg)
    {
        _gemini = gemini;
        _cfg = cfg;
    }

    public async Task<string?> ReplyAsync(WebhookDeliveryMessage msg, CancellationToken ct)
    {
        if (msg.Text?.Body is null) return null;

        var systemPrompt = _cfg["WAFlowBot:Gemini:SystemPrompt"]
                           ?? "You are a concise, helpful WhatsApp-style assistant." + _waFlowContext;

        return await _gemini.GenerateAsync(systemPrompt, msg.Text.Body, ct);
    }
}