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

## Run the Worker (service broker)

```bash
cd backend
dotnet run --project src/FinancePlatform.Worker
```

The Worker loads queue configuration from `appsettings.json` (`Broker:Queues`).
Trigger claim/execute loops are stubs until Phase 2.

## Current phase

**Phase 0 — Foundations:** solution, conventions, Api/Worker hosts, smoke tests.
Domain models and the trigger engine start in Phase 1–2.
