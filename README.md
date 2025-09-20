# WAFlow (MVP)

**A minimal local sandbox to build and test webhook-based WhatsApp-like bots.**

Stop waiting for deployments. WAFlow lets you spin up a complete local environment with one command.
Chat in the browser UI, see your webhook get hit instantly, and use conversation replay for lightning-fast regression testing.

## Key Features

- Instant Setup: Go from zero to a running sandbox with a single docker compose up.
- Conversation Replay: Export conversations to JSON and import them later for quick regression testing.
- Local & Offline: Develop and test your bot entirely on your machine, no internet or external services required.
- Simple & Focused: No complex platforms. Just a clean UI, a simulator API, and your webhook.

---

## Architecture
```
./WAFlow.Simulator   → Minimal API (.NET): inject user messages, dispatch webhooks, store feed
./WAFlow.Chat          → Blazor UI: chat (polling), export/import, reset, online banner
./EchoBot.Example     → Example webhook (Minimal API): echoes inbound text
./docker-compose.yml
```

**Ports (host & local dev):** UI `5056`, Simulator `5080`, Echobot `5199`.

> In Docker, services talk to each other via service DNS on port 8080:
> `http://sim:8080` and `http://echobot:8080`.

---

## Requirements
- Docker Desktop
- (Optional) .NET 8 SDK if you want to run without Docker

---

## Quickstart (60s)
```bash
git clone <your-repo>.git waflow
cd waflow
docker compose up -d --build
```

Open **http://localhost:5056/** and start chatting. 
Tail logs:
```bash
docker compose logs -f sim
docker compose logs -f echobot

```

The UI shows **ONLINE** when the simulator is healthy. You can **Export**, **Import**, and **Reset** from the UI.

---

## Run without Docker
Run three terminals:

**1) Simulator**
```bash
cd WAFlow.Simulator
dotnet run --urls http://localhost:5080
```

**2) Echobot (example)**
```bash
cd EchoBot.Example
dotnet run --urls http://localhost:5199
```

**3) UI**
```bash
cd WAFlow.Chat
dotnet run --urls http://localhost:5056
```

---

## Replay a JSON transcript
**From the UI:** click **Import** and choose a previously exported `.json` transcript.


---

## Hook up your webhook

**A) Auto-register (Docker) — recommended**
In docker-compose.yml, the Echobot self-registers on startup:
```yaml
echobot:
  environment:
    WAFlowBot__SimulatorBaseUrl: http://sim:8080
    WAFlowBot__MyWebhookUrl:     http://echobot:8080/bot/webhook
```
**B) Manual register (any environment)**
```bash
curl -X POST "http://localhost:5080/webhook/register" \
  -H "Content-Type: application/json" \
  -d '{"url":"http://localhost:5199/bot/webhook"}'
```

Check what’s stored
```bash
curl "http://localhost:5080/webhook"
# Delete:
curl -X DELETE "http://localhost:5080/webhook"
```
---

## Limitations (MVP)
- **No SignalR**: UI uses **polling** (no push updates yet).
- **Single user** in the UI (no userId switcher).
- **Text messages only** (model ready to expand to images/buttons/lists, not exposed yet).
- **Single global webhook**, no auth.
- **Export/Import schema v0** (may change slightly before v0.1).

---

## Short roadmap
1) **SignalR** for real-time feed and events (`messageAdded`, `messagesCleared`).
2) **User selector** with per-user feeds.
3) **Richer message types** (image, buttons, lists) + basic UI rendering.
4) **CLI replay runner** (execute N transcripts and compare outputs).
5) **Per-user webhooks** + HMAC signing and retries.
6) **Logs panel** in UI: delivered, retries, HTTP errors, reconnections.
7) **Integration tests** and data fixtures.

---



## Troubleshooting
- **UI shows OFFLINE** → check docker compose logs -f sim and verify Simulator__BaseUrl (http://sim:8080 in Docker, http://localhost:5080 locally).
- **Webhook not receiving** → verify it’s stored (GET /webhook) or (re)register via POST /webhook/register. In Docker the URL must be http://echobot:8080/bot/webhook.
- **Inter-service calls** → never use localhost between containers; use sim:8080 and echobot:8080.
---

## License
MIT 
