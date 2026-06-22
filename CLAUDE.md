# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

**ApiMonitoringPlatform** is a monitoring platform project. Currently it contains one component:

- **FakeTargetApi** — a .NET 9 minimal API that acts as the monitored target. It exposes realistic endpoints with configurable simulation of latency and failures, intended to be observed by a monitoring layer (not yet implemented in this repo).

## Commands

All commands run from `FakeTargetApi/`.

```bash
# Run locally (http://localhost:5067)
dotnet run

# Build
dotnet build

# Publish release binary
dotnet publish -c Release -o ./publish

# Run via Docker Compose (port 8080)
docker-compose up --build
```

There are no tests yet.

## FakeTargetApi architecture

Single-file minimal API (`Program.cs`). All business logic lives inline — no controllers, no service layer, no repositories.

### Simulation middleware

A `app.Use(...)` middleware (registered before routes) reads two config values on every request:

- `Simulation:DelayMs` — adds a fixed delay to all requests
- `Simulation:FailureRate` — probability (0.0–1.0) of returning HTTP 500 instead of proceeding

This short-circuits *before* the route handler runs, so even authenticated routes get the failure injected.

### Per-endpoint simulation

Two endpoints have their own additional simulation on top of the global middleware:

- `GET /reports` — adds `Simulation:ReportsExtraDelayMs` (default 3000 ms) to simulate a slow computation
- `GET /external-data` — uses `Simulation:ExternalFailureRate` (default 0.3) to randomly return HTTP 503

### Auth

`POST /auth/token` issues a HS256 JWT using credentials from config (`Jwt:TestUser` / `Jwt:TestPassword`, defaults `testuser` / `testpass`). The token is required as a `Bearer` header for `GET /orders`. All other endpoints are public.

### Simulation tuning

Override via environment variables (Docker double-underscore convention) or `appsettings.json`:

| Config key | Default | Effect |
|---|---|---|
| `Simulation:DelayMs` | 0 | Global fixed delay (ms) on every request |
| `Simulation:FailureRate` | 0.0 | Global random 500 rate (0–1) |
| `Simulation:ReportsExtraDelayMs` | 3000 | Extra delay on `/reports` |
| `Simulation:ExternalFailureRate` | 0.3 | Random 503 rate on `/external-data` |

### In-memory state

Products are held in a `List<Product>` seeded with two items. State resets on restart; Docker Compose has no volume for persistence.
