# ChronosDLQ

ChronosDLQ is a local-first Dead Letter Queue surgeon dashboard for RabbitMQ.

It helps you connect to a broker, watch DLQ queues, inspect poisoned messages, edit bad JSON payloads, replay fixed messages back to the target queue, or discard traces that are no longer useful.

This is built as a developer tool first: run it close to the broker you are debugging, keep the message data on your machine, and avoid turning operational DLQ data into shared SaaS state.

## What It Does

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

## Why Local-First?

RabbitMQ queues often contain private operational data, customer payloads, internal identifiers, or failure details that should stay close to the system being debugged.

ChronosDLQ is designed to run as a local Docker tool so each developer or team can have an isolated UI, API, database, and broker connection. That keeps the workflow simple and avoids mixing queue state across users.

```text
your machine
  -> ChronosDLQ UI
  -> ChronosDLQ API
  -> local Postgres
  -> your RabbitMQ
```

Hosted multi-tenant deployment is a different product shape and would need separate decisions around auth, tenant isolation, credentials, auditing, and data retention. This project intentionally starts with the local developer workflow.

## Stack

- .NET API
- React + Vite UI
- RabbitMQ
- Postgres
- Docker Compose
- Serilog
- EF Core

## Run Without Cloning

If you just want to run ChronosDLQ from the published container images:

```powershell
curl.exe -L -o chronosdlq.compose.yml https://raw.githubusercontent.com/nosaemma21/ChronosDLQ/main/docker-compose.release.yml; docker compose -f chronosdlq.compose.yml up
```

Then open:

```text
http://localhost:5173
```

## Run From Source

If you cloned the repo and want to build the containers locally:

```powershell
docker compose up --build
```

Then open:

```text
http://localhost:5173
```

## Local RabbitMQ

The bundled RabbitMQ Management UI is available at:

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

The app handles Docker networking internally.

## Useful Local URLs

```text
UI:        http://localhost:5173
API:       http://localhost:5103
Health:    http://localhost:5103/api/health
RabbitMQ:  http://localhost:15672
Postgres:  localhost:5432
```

## Cloud RabbitMQ

For CloudAMQP or another hosted broker, paste the full AMQP URL into ChronosDLQ:

```text
amqps://user:password@host/vhost
```

For the cleanest replay test, watch the DLQ queue, not the normal target queue.

Example:

```text
watch:  my_queue.dlq
replay: my_queue
```

If you watch the normal queue itself, ChronosDLQ will consume messages from that queue because watching is an active RabbitMQ consumer.

## Local Dev Keys

For local Docker, the compose file uses simple development keys:

```text
some_api_key
some_chronos_operator_key
```

These are fine for local development. Do not expose this compose setup as a shared public service with those keys.

## Logs

Follow all container logs:

```powershell
docker compose logs -f
```

Follow only the API logs:

```powershell
docker compose logs -f chronos-api
```

## Stop Everything

```powershell
docker compose down
```

To remove local volumes too:

```powershell
docker compose down -v
```

That deletes the local RabbitMQ/Postgres data.
