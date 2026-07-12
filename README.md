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

## Run the Worker (service broker)

```bash
cd backend
dotnet run --project src/FinancePlatform.Worker
```

The Worker loads queue configuration from `appsettings.json` (`Broker:Queues`).
On startup it seeds a sample deposit → buy workflow (disable with
`Broker:SeedSampleWorkflowOnStartup=false`).

Persistence defaults to **InMemory**. To use SQL Server:

1. Apply scripts under `db/SqlServer/` (`Tables/`, `Archives/`, `Procedures/`)
2. Set `Persistence:Provider` to `SqlServer`
3. Set `ConnectionStrings:FinancePlatform`

## Current phase

**Phase 3 — SQL persistence:** schema, archive tables (`*_a`), `get_<Model>_f` /
`<Model>_u` SPs, Dapper repositories, `SqlTriggerStore`. Trigger tables have no
archives; broker sets `ChangedBy = broker`.

