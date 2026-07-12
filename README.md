# Finance Platform — Backend

.NET 10 solution for the asset allocation service broker described in
`AssetAllocationServiceBrokerArchitecture_v2.md` and
`AssetAllocation_DotNet_BuildPlan.md`.

## Solution layout

```
backend/
├── FinancePlatform.slnx
├── Directory.Build.props
├── .editorconfig
├── src/
│   ├── FinancePlatform.Models
│   ├── FinancePlatform.Data
│   ├── FinancePlatform.Services
│   ├── FinancePlatform.Api          # HTTP entry points
│   └── FinancePlatform.Worker       # console service broker / batch host
├── tests/
│   ├── FinancePlatform.UnitTests
│   ├── FinancePlatform.IntegrationTests
│   └── FinancePlatform.ComponentTests
└── db/
    └── SqlServer/
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Build

```bash
cd backend
dotnet build FinancePlatform.slnx
```

## Run tests

```bash
cd backend
dotnet test FinancePlatform.slnx
```

## Run the API

```bash
cd backend
dotnet run --project src/FinancePlatform.Api
```

- Root: `GET /`
- Health: `GET /health`
- OpenAPI document: `GET /openapi/v1.json`
- Scalar API docs: `/scalar`
- Enqueue workflows:
  - `POST /api/workflows/deposits`
  - `POST /api/workflows/buys`
  - `POST /api/workflows/sells`
  - `POST /api/workflows/allocations` (6002 → … → 9001)

With `Persistence:Provider=InMemory`, API and Worker do **not** share a store (separate processes).
Use `SqlServer` for API → Worker end-to-end, or rely on the Worker sample seed / unit tests for local demos.

## Run the Worker (service broker)

```bash
cd backend
dotnet run --project src/FinancePlatform.Worker
```

The Worker loads queue configuration from `appsettings.json` (`Broker:Queues`).
On startup it seeds a sample deposit → buy workflow (disable with
`Broker:SeedSampleWorkflowOnStartup=false`).

Persistence defaults to **InMemory**. To use SQL Server:

1. Deploy scripts: `db/SqlServer/Deploy-SqlServer.ps1` (or apply `Tables/`, `Archives/`, `Procedures/` manually)
2. Set `Persistence:Provider` to `SqlServer`
3. Set `ConnectionStrings:FinancePlatform`

## Current phase

**Phase 6 — Heartbeats, recovery & worker hardening:**

- Queue heartbeat (throttled logs) + trigger lease refresh while EPs run
- `TriggerRecoveryService` / `RecoverExpiredTriggers` SP: expired leases → Pending
- Config: poll/concurrency/retry + recovery/heartbeat intervals under `Broker`
- Correlation / root workflow / trigger id on execution logs via `BeginScope`
- Heartbeat failure marks worker unhealthy and pauses claims
