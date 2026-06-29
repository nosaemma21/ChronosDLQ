# ChronosDLQ 🩺🐇

ChronosDLQ is a local Dead Letter Queue surgeon dashboard.

It helps me connect to RabbitMQ, watch DLQ queues, inspect poisoned messages, edit bad payloads, replay them back to the real queue, or discard them when they are truly dead 💀

This is built as a dev tool first.

Not SaaS.
Not shared multi-tenant magic.
Just run it beside your RabbitMQ setup and fix your messages in peace.

## What it does ✨

- Watch selected RabbitMQ queues
- Show active poison messages
- Inspect failed payloads
- Edit JSON payloads before replay
- Replay messages back to their target queue
- Discard messages from the dashboard
- Preserve RabbitMQ metadata during replay
- Store indexed messages in Postgres so they survive API restarts
- Add audit logs for replay and discard actions
- Keep sensitive payloads out of normal logs

## Why local Docker? 🐳

Because RabbitMQ queues are private operational data.

If this was hosted as one shared public app, users could accidentally share the same backend, same DB, same queue state, or same watched message index. That needs proper auth, tenant isolation, permission boundaries, and separate config per user.

So for now, ChronosDLQ is meant to run locally:

```text
your machine
  -> ChronosDLQ UI
  -> ChronosDLQ API
  -> local Postgres
  -> your RabbitMQ
```

Each developer gets their own app and their own DB.

## Stack 🧱

- .NET API
- React + Vite UI
- RabbitMQ
- Postgres
- Docker Compose
- Serilog
- EF Core

## Run with Docker 🚀

If you just want to run ChronosDLQ without cloning the repo:

```powershell
docker compose -f https://raw.githubusercontent.com/nosaemma21/ChronosDLQ/main/docker-compose.release.yml up
```

Then open:

```text
http://localhost:5173
```

If you cloned the repo and want to build from source, run this from the project root:

```powershell
docker compose up --build
```

Then open:

```text
http://localhost:5173
```

RabbitMQ Management UI is available at:

```text
http://localhost:15672
```

Default RabbitMQ login:

```text
guest / guest
```

In ChronosDLQ, connect with:

```text
amqp://localhost:5672
```

The app will handle the Docker networking part internally.

## Useful local URLs 🔌

```text
UI:        http://localhost:5173
API:       http://localhost:5103
Health:    http://localhost:5103/api/health
RabbitMQ:  http://localhost:15672
Postgres:  localhost:5432
```

## API keys 🔐

For local Docker, the compose file uses simple dev keys:

```text
some_api_key
some_chronos_operator_key
```

That is fine for local development.

Do not expose this as a shared public service with those keys.

## Stop everything 🛑

```powershell
docker compose down
```

To remove local volumes too:

```powershell
docker compose down -v
```

That deletes the local RabbitMQ/Postgres data.

## Current direction 🧭

ChronosDLQ is currently a local developer tool.

If it ever becomes hosted, it needs proper multi-user isolation first:

- auth
- tenant scoped queues
- per-user RabbitMQ config
- per-user DB/message index
- strict replay/discard permissions
- stronger audit trails

Until then, Docker is the right home for it.
