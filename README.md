# WAFlow (MVP)

**A minimal local sandbox to build and test webhook-based chatbots.**

Stop waiting for deployments. WAFlow lets you spin up a complete local environment with one command.
Chat in the browser UI, see your webhook get hit instantly, and use conversation replay for lightning-fast regression testing.

![WAFlow](docs/assets/waflowDemoGif.gif)

---

## Key Features

- Instant Setup: Go from zero to a running sandbox with a single docker compose up.
- Conversation Replay: Export conversations to JSON and import them later for quick regression testing.
- Local & Offline: Develop and test your bot entirely on your machine, no internet or external services required.
- Simple & Focused: No complex platforms. Just a clean UI, a simulator API, and your webhook.

---

## Architecture
```
./WAFlow.Simulator   → Minimal API (.NET): inject user messages, dispatch webhooks, store feed, SignalR hub at /stream
./WAFlow.Chat        → Blazor Server UI: chat (SignalR push), export/import, reset, online banner
./LlmBot.Example     → Example Bot with LLM: Receives a webhook and answers using Gemini
./docker-compose.yml
```

**Ports (host & local dev):** UI `5056`, Simulator `5080`, LlmBot `5199`.

> In Docker, services talk to each other via service DNS on port 8080:
> `http://sim:8080` and `http://llmbot:8080`.

### How messages reach the browser

There are two SignalR hops. The UI is Blazor Server, so the browser is connected to the UI process
over a Blazor circuit. The UI process in turn holds a `HubConnection` to the Simulator's `/stream`
hub and joins a group named after the user id. When the Simulator stores a message it pushes it to
that group, the UI process receives it and re-renders the circuit, and the browser updates. Neither
hop pins a transport: both negotiate, which means WebSockets in practice with the usual fallback to
Server-Sent Events or long polling if the upgrade fails. There is no polling loop — the UI does one
`GET /messages` on startup to backfill history, and everything after that is pushed.

---

## Requirements
- Docker Desktop
- (Optional) .NET 8 SDK if you want to run without Docker
- (Optional, only to use the example LlmBot) Gemini Api key
---

## Quickstart 
```bash
git clone <your-repo>.git waflow
cd waflow
# create .env 
echo "GOOGLE_API_KEY=your_gemini_key" > .env
docker compose up -d --build
```

Open **http://localhost:5056/** and start chatting. 
Tail logs:
```bash
docker compose logs -f sim
docker compose logs -f llmbot

```

The UI shows **ONLINE** when the simulator is healthy. You can **Export**, **Import**, and **Reset** from the UI.

If the UI shows OFFLINE, ensure sim is healthy and that the bot registered its webhook (see logs of sim and llmbot).

---


## Replay a JSON transcript
**From the UI:** click **Import** and choose a previously exported `.json` transcript.


---

## Hook up your webhook

**A) Auto-register (Docker) — recommended**
In docker-compose.yml, the llmbot self-registers on startup:
```yaml
llmbot:
  environment:
    WAFlowBot__SimulatorBaseUrl: http://sim:8080
    WAFlowBot__MyWebhookUrl:     http://llmbot:8080/bot/webhook
```
**B) Manual register (any environment)**
```bash
curl -X POST "http://localhost:5080/webhook/register" \
  -H "Content-Type: application/json" \
  -d '{"botId":"my-bot","webhookUrl":"http://localhost:5199/bot/webhook"}'
```
`botId` and `webhookUrl` are both required. An optional `secret` (16–64 chars) is sent back to your
bot on every delivery as the `X-WAFlow-Secret` header.

Check what’s stored
```bash
curl "http://localhost:5080/webhook"
# Delete:
curl -X DELETE "http://localhost:5080/webhook"
```
---

## Relationship to the WhatsApp Cloud API

WhatsApp inspired the message shape: a delivery carries a `messages` array whose entries have
`from`, `type`, `text.body` and `timestamp`, which reads much like the inner `value.messages[]`
of a Meta webhook. The resemblance stops there, and that is deliberate — this is a local sandbox
for exercising your own webhook, not a Meta emulator.

Concretely, WAFlow payloads are **not** Meta Cloud API compatible:

- No `entry` / `changes` / `value` envelope, no `object`, no `messaging_product`, no `metadata` or `contacts`.
- No `hub.challenge` subscribe-time verification (the bot exposes `POST` only).
- Deliveries are authenticated with a custom `X-WAFlow-Secret` header, not `X-Hub-Signature-256`.
- `timestamp` is ISO-8601; Meta sends Unix seconds as a string. `type` serializes as `"Text"`, not `"text"`.

A delivery looks like this:
```json
{ "waFlowVersion": "v0",
  "messages": [ { "from": "user-001", "type": "Text",
                  "text": { "body": "hi" },
                  "timestamp": "2026-07-19T12:00:00+00:00" } ] }
```

---

## Limitations (MVP)
- **Single user** in the UI (no userId switcher); the feed is a shared singleton.
- **Text messages only** (model ready to expand to images/buttons/lists, not exposed yet).
- **Single global webhook.** Auth is an optional shared secret sent as `X-WAFlow-Secret`; there is no
  signing, and the example bot logs a mismatch but still processes the message.
- **Delivery is one attempt, fire-and-forget.** No retry and no explicit timeout (stock `HttpClient`
  default). Failures are logged and reported in the `/simulate/user` response, not queued.

---

## Short roadmap
1) **User selector** with per-user feeds.
2) **Richer message types** (image, buttons, lists) + basic UI rendering.
3) **CLI replay runner** (execute N transcripts and compare outputs).
4) **Per-user webhooks** + HMAC signing and retries.
5) **Delivery detail in the UI logs panel**: per-delivery status, retries, HTTP errors.
6) **Integration tests** and data fixtures.

---



## Troubleshooting
- **UI shows OFFLINE** → check docker compose logs -f sim and verify Simulator__BaseUrl (http://sim:8080 in Docker, http://localhost:5080 locally).
- **Webhook not receiving** → verify it’s stored (GET /webhook) or (re)register via POST /webhook/register. In Docker the URL must be http://llmbot:8080/bot/webhook.
- **Inter-service calls** → never use localhost between containers; use sim:8080 and llmbot:8080.
---

## License
MIT 
