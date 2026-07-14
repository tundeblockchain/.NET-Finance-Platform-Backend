using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class CashOperationsRepository(IDbConnectionFactory connectionFactory) : ICashOperationsRepository
{
    public async Task<CashBalance?> GetBalanceByAccountCurrencyAsync(
        Guid accountId,
        string currency,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CashBalance>(
            new CommandDefinition(
                CashProcedureNames.GetCashBalanceByAccountCurrency,
                new { AccountId = accountId, Currency = currency.ToUpperInvariant() },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<CashBalance> EnsureBalanceAsync(
        Guid accountId,
        string currency,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetBalanceByAccountCurrencyAsync(accountId, currency, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        // Create via lock acquire (creates zero balance if missing), then release so callers aren't left locked.
        var systemTrigger = Guid.NewGuid();
        var created = await AcquireLockAsync(
            accountId,
            currency,
            systemTrigger,
            allocationRequestId: null,
            leaseSeconds: 5,
            changedBy,
            cancellationToken);

        if (created is null)
        {
            return await GetBalanceByAccountCurrencyAsync(accountId, currency, cancellationToken)
                ?? throw new InvalidOperationException("Unable to ensure cash balance.");
        }

        await ReleaseLockAsync(accountId, currency, systemTrigger, changedBy, cancellationToken);
        return await GetBalanceByAccountCurrencyAsync(accountId, currency, cancellationToken)
            ?? throw new InvalidOperationException("Unable to ensure cash balance.");
    }

    public async Task<CashBalance?> AcquireLockAsync(
        Guid accountId,
        string currency,
        Guid triggerId,
        Guid? allocationRequestId,
        int leaseSeconds,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CashBalance>(
            new CommandDefinition(
                CashProcedureNames.AcquireCashLock,
                new
                {
                    AccountId = accountId,
                    Currency = currency.ToUpperInvariant(),
                    TriggerId = triggerId,
                    AllocationRequestId = allocationRequestId,
                    LeaseSeconds = leaseSeconds,
                    ChangedBy = changedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<bool> ReleaseLockAsync(
        Guid accountId,
        string currency,
        Guid triggerId,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rowsAffected = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                CashProcedureNames.ReleaseCashLock,
                new
                {
                    AccountId = accountId,
                    Currency = currency.ToUpperInvariant(),
                    TriggerId = triggerId,
                    ChangedBy = changedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return rowsAffected > 0;
    }

    public async Task<(CashBalance Balance, bool AlreadyApplied)> DepositAsync(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        Guid triggerId,
        Guid? allocationRequestId,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleAsync<CashBalanceWithFlag>(
            new CommandDefinition(
                CashProcedureNames.DepositCash,
                new
                {
                    IdempotencyKey = idempotencyKey,
                    AccountId = accountId,
                    Currency = currency.ToUpperInvariant(),
                    Amount = amount,
                    TriggerId = triggerId,
                    AllocationRequestId = allocationRequestId,
                    ChangedBy = changedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return (row.ToBalance(), row.AlreadyApplied);
    }

    public Task<(CashBalance Balance, CashReservation Reservation, bool AlreadyApplied)> ReserveAsync(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        Guid triggerId,
        Guid allocationRequestId,
        string changedBy,
        CancellationToken cancellationToken = default) =>
        ExecuteBalanceAndReservationAsync(
            CashProcedureNames.ReserveCash,
            new
            {
                IdempotencyKey = idempotencyKey,
                AccountId = accountId,
                Currency = currency.ToUpperInvariant(),
                Amount = amount,
                TriggerId = triggerId,
                AllocationRequestId = allocationRequestId,
                ChangedBy = changedBy
            },
            cancellationToken);

    public Task<(CashBalance Balance, CashReservation Reservation, bool AlreadyApplied)> ReleaseReservationAsync(
        string idempotencyKey,
        Guid triggerId,
        string changedBy,
        CancellationToken cancellationToken = default) =>
        ExecuteBalanceAndReservationAsync(
            CashProcedureNames.ReleaseCashReservation,
            new
            {
                IdempotencyKey = idempotencyKey,
                TriggerId = triggerId,
                ChangedBy = changedBy
            },
            cancellationToken);

    public Task<(CashBalance Balance, CashReservation Reservation, bool AlreadyApplied)> ConsumeReservationAsync(
        string idempotencyKey,
        Guid triggerId,
        string changedBy,
        CancellationToken cancellationToken = default) =>
        ExecuteBalanceAndReservationAsync(
            CashProcedureNames.ConsumeCashReservation,
            new
            {
                IdempotencyKey = idempotencyKey,
                TriggerId = triggerId,
                ChangedBy = changedBy
            },
            cancellationToken);

    private async Task<(CashBalance Balance, CashReservation Reservation, bool AlreadyApplied)> ExecuteBalanceAndReservationAsync(
        string procedure,
        object parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                procedure,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var balanceRow = await multi.ReadSingleAsync<CashBalanceWithFlag>();
        var reservation = await multi.ReadSingleAsync<CashReservation>();
        return (balanceRow.ToBalance(), reservation, balanceRow.AlreadyApplied);
    }

    private sealed class CashBalanceWithFlag
    {
        public Guid Id { get; init; }
        public Guid AccountId { get; init; }
        public string Currency { get; init; } = "";
        public decimal Settled { get; init; }
        public decimal Reserved { get; init; }
        public bool IsLocked { get; init; }
        public Guid? LockedByAllocationId { get; init; }
        public Guid? LockedByTriggerId { get; init; }
        public DateTimeOffset? LockAcquiredUtc { get; init; }
        public DateTimeOffset? LockExpiresUtc { get; init; }
        public DateTimeOffset DateModified { get; init; }
        public string ChangedBy { get; init; } = "";
        public bool AlreadyApplied { get; init; }

        public CashBalance ToBalance() => new()
        {
            Id = Id,
            AccountId = AccountId,
            Currency = Currency,
            Settled = Settled,
            Reserved = Reserved,
            IsLocked = IsLocked,
            LockedByAllocationId = LockedByAllocationId,
            LockedByTriggerId = LockedByTriggerId,
            LockAcquiredUtc = LockAcquiredUtc,
            LockExpiresUtc = LockExpiresUtc,
            DateModified = DateModified,
            ChangedBy = ChangedBy
        };
    }
}
