# SQL Server scripts

## Deploy (recommended)

From this folder, with `sqlcmd` on PATH:

```powershell
.\Deploy-SqlServer.ps1
```

Options:

```powershell
# Named instance
.\Deploy-SqlServer.ps1 -Server "localhost\SQLEXPRESS"

# SQL authentication
.\Deploy-SqlServer.ps1 -Server "myserver" -Username sa -Password "YourPassword"

# Skip create-database (DB already exists)
.\Deploy-SqlServer.ps1 -SkipCreateDatabase

# Preview only
.\Deploy-SqlServer.ps1 -WhatIf
```

The script applies scripts in this order:

1. `00_CreateDatabase.sql`
2. `Tables/*.sql` — one file per main table
3. `Archives/*.sql` — one file per `*_a` archive table
4. `Procedures/**/*.sql` — one file per stored procedure
   (include `Procedures/Cash/` for AcquireCashLock, DepositCash, ReserveCash, …)

## Layout

```
SqlServer/
├── 00_CreateDatabase.sql
├── Tables/           # Account.sql, TradingAccount.sql, InvestmentAccount.sql, InvestmentInstruction.sql, …
├── Archives/         # Account_a.sql, InvestmentAccount_a.sql, InvestmentInstruction_a.sql, …
└── Procedures/
    ├── Account/
    ├── InvestmentAccount/       # Ensure, Credit/Debit, get_*, _u
    ├── InvestmentInstruction/   # Create, SetOrderId, UpdateStatus, get_*
    ├── CustomerOps/             # ProvisionCustomer, Credit/Debit*, EnsureTradingToInvestmentDistribution
    ├── …
    └── Triggers/
```

Investment buy path persistence:

| Object | Purpose |
|--------|---------|
| `InvestmentAccount` | Cash parked for investment after Trading distribute |
| `InvestmentInstruction` | Trading-created order details (asset, qty, amount, side) |
| `EnsureInvestmentAccount` | Lazy-create investment account for a trading account |
| `EnsureTradingToInvestmentDistribution` | Agreement + element (target 802) |
| `CreateInvestmentInstruction` | Idempotent instruction insert |
## Conventions

| Object | Pattern | Example |
|--------|---------|---------|
| Table | `<Model>` | `Account` |
| Archive | `<Model>_a` | `Account_a` |
| Fetch | `get_<Model>_f` | `get_Account_f` |
| Upsert | `<Model>_u` | `Account_u` |

`<Model>_u` archives the current row into `<Model>_a` before updating.
Broker mutations set `ChangedBy = 'broker'`.

**No archive tables** for `SystemEventTrigger` or `SystemEventWorking`.
