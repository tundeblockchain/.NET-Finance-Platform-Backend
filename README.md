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

API controller unit tests live under `tests/FinancePlatform.UnitTests/Api/` (Customers, Trading, Workflows, Meta).

## Run the API

```bash
cd backend
dotnet run --project src/FinancePlatform.Api
```

Ports are set in `src/FinancePlatform.Api/appsettings.json` (`Urls` / `Api:HttpPort` / `Api:HttpsPort`) and mirrored in `Properties/launchSettings.json`.

- API (HTTP): `http://localhost:5152`
- API (HTTPS): `https://localhost:7245`
- **Scalar docs:** [http://localhost:5152/scalar](http://localhost:5152/scalar) (or `https://localhost:7245/scalar`)
- OpenAPI document: `http://localhost:5152/openapi/v1.json`
- Health: `http://localhost:5152/health`

`dotnet run` can open Scalar automatically (`launchBrowser` + `launchUrl: scalar` in launch settings).

- Customer APIs:
  - `POST /api/customers` — create customer + accounts + park agreement
  - `GET /api/customers/{id}` — customer and account balances
  - `POST /api/customers/{id}/deposits` — Deposit funds (6001)
  - `POST /api/customers/{id}/distribute-to-trading` — Transfer funds to trading (6002 → 7001 park)
  - `GET /api/customers/{id}/trading-account` — parked trading balance
- Trading APIs (trading account only):
  - `GET /api/trading/customers/{id}/funds` — cash + positions
  - `GET /api/trading/customers/{id}/positions`
  - `GET /api/trading/customers/{id}/history` — trade history
  - `POST /api/trading/customers/{id}/buys`
  - `POST /api/trading/customers/{id}/sells`
  - `POST /api/trading/customers/{id}/transfer-to-customer` — Transfer funds to customer (7003 → 6003)
- Legacy workflows: `POST /api/workflows/deposits|sells` (1001 / 2003)

With `Persistence:Provider=InMemory`, API and Worker do **not** share a store (separate processes).
Use `SqlServer` for API → Worker end-to-end (customer directory uses SPs via `SqlCustomerDirectory`).

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

**Customer money path (park-only) + Phase 6 hardening:**

- Create customer → CustomerAccount + TradingAccount + distribution element (target type 702)
- `6001` deposit into CustomerAccount; `6002` distribute → `7001` Trading.Receive **parks only** (no auto-invest)
- Trading UI (later) invests from the trading account
- Queue heartbeat + lease recovery (Phase 6) still apply
