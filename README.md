# ChronosDLQ 🩺🐇

ChronosDLQ is a Dead Letter Queue surgeon dashboard.

It connects to RabbitMQ, watches the queues I care about, pulls poisoned messages into a dashboard, lets me inspect what failed, edit the payload, replay it back to the real queue, or discard it when it is truly dead 💀

Built because debugging DLQs from the RabbitMQ UI alone is kind of tasking.

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
- Keep sensitive payloads out of production logs

## Stack 🧱

- .NET API
- React + Vite UI
- RabbitMQ
- Postgres
- Docker Compose
- Serilog
- EF Core

## Running locally 🚀

Start RabbitMQ and Postgres:

```powershell
docker compose up -d