using FinancePlatform.Data.DataLayer;
using FinancePlatform.Models;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Services.Cash;

/// <summary>
/// SQL Server-backed cash service using Acquire/Deposit/Reserve/Consume stored procedures.
/// </summary>
public sealed class SqlCashService(ICashOperationsRepository cashOps) : ICashService
{
    public CashBalance GetOrCreateBalance(Guid accountId, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        return cashOps
            .EnsureBalanceAsync(accountId, currency.ToUpperInvariant(), ChangeActors.Broker)
            .GetAwaiter()
            .GetResult();
    }

    public decimal GetAvailable(Guid accountId, string currency)
    {
        var balance = cashOps
            .GetBalanceByAccountCurrencyAsync(accountId, currency.ToUpperInvariant())
            .GetAwaiter()
            .GetResult();
        return balance?.Available ?? 0m;
    }

    public decimal GetSettled(Guid accountId, string currency)
    {
        var balance = cashOps
            .GetBalanceByAccountCurrencyAsync(accountId, currency.ToUpperInvariant())
            .GetAwaiter()
            .GetResult();
        return balance?.Settled ?? 0m;
    }

    public CashLockResult TryAcquireLock(
        Guid accountId,
        string currency,
        Guid triggerId,
        Guid? allocationRequestId,
        TimeSpan leaseDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (triggerId == Guid.Empty)
        {
            throw new ArgumentException("Trigger id is required.", nameof(triggerId));
        }

        var leaseSeconds = Math.Max(1, (int)Math.Ceiling(leaseDuration.TotalSeconds));
        var balance = cashOps
            .AcquireLockAsync(
                accountId,
                currency.ToUpperInvariant(),
                triggerId,
                allocationRequestId,
                leaseSeconds,
                ChangeActors.Broker)
            .GetAwaiter()
            .GetResult();

        return balance is null
            ? CashLockResult.Contended()
            : CashLockResult.Acquired(balance);
    }

    public bool TryReleaseLock(Guid accountId, string currency, Guid triggerId) =>
        cashOps
            .ReleaseLockAsync(accountId, currency.ToUpperInvariant(), triggerId, ChangeActors.Broker)
            .GetAwaiter()
            .GetResult();

    public CashMutationResult TryDeposit(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        Guid triggerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (amount <= 0)
        {
            return CashMutationResult.Fail("Deposit amount must be positive.");
        }

        try
        {
            var (balance, alreadyApplied) = cashOps
                .DepositAsync(
                    idempotencyKey,
                    accountId,
                    currency.ToUpperInvariant(),
                    amount,
                    triggerId,
                    allocationRequestId: null,
                    ChangeActors.Broker)
                .GetAwaiter()
                .GetResult();

            return alreadyApplied
                ? CashMutationResult.Duplicate(balance)
                : CashMutationResult.Success(balance);
        }
        catch (Exception ex)
        {
            return CashMutationResult.Fail(RootMessage(ex));
        }
    }

    public CashMutationResult TryReserve(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        Guid triggerId,
        Guid allocationRequestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (amount <= 0)
        {
            return CashMutationResult.Fail("Reservation amount must be positive.");
        }

        try
        {
            var (balance, reservation, alreadyApplied) = cashOps
                .ReserveAsync(
                    idempotencyKey,
                    accountId,
                    currency.ToUpperInvariant(),
                    amount,
                    triggerId,
                    allocationRequestId,
                    ChangeActors.Broker)
                .GetAwaiter()
                .GetResult();

            return alreadyApplied
                ? CashMutationResult.Duplicate(balance, reservation)
                : CashMutationResult.Success(balance, reservation);
        }
        catch (Exception ex)
        {
            return MapInsufficientCash(RootMessage(ex), accountId, currency, amount);
        }
    }

    public CashMutationResult TryReleaseReservation(string idempotencyKey, Guid triggerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        try
        {
            var (balance, reservation, alreadyApplied) = cashOps
                .ReleaseReservationAsync(idempotencyKey, triggerId, ChangeActors.Broker)
                .GetAwaiter()
                .GetResult();

            return alreadyApplied
                ? CashMutationResult.Duplicate(balance, reservation)
                : CashMutationResult.Success(balance, reservation);
        }
        catch (Exception ex)
        {
            return CashMutationResult.Fail(RootMessage(ex));
        }
    }

    public CashMutationResult TryConsumeReservation(string idempotencyKey, Guid triggerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        try
        {
            var (balance, reservation, alreadyApplied) = cashOps
                .ConsumeReservationAsync(idempotencyKey, triggerId, ChangeActors.Broker)
                .GetAwaiter()
                .GetResult();

            return alreadyApplied
                ? CashMutationResult.Duplicate(balance, reservation)
                : CashMutationResult.Success(balance, reservation);
        }
        catch (Exception ex)
        {
            return CashMutationResult.Fail(RootMessage(ex));
        }
    }

    private CashMutationResult MapInsufficientCash(
        string message,
        Guid accountId,
        string currency,
        decimal amount)
    {
        if (message.Contains("Insufficient available cash", StringComparison.OrdinalIgnoreCase)
            && !message.Contains("Available=", StringComparison.Ordinal))
        {
            var available = GetAvailable(accountId, currency);
            return CashMutationResult.Fail(
                $"Insufficient available cash. Available={available}, requested={amount}.");
        }

        return CashMutationResult.Fail(message);
    }

    private static string RootMessage(Exception ex)
    {
        while (ex.InnerException is not null)
        {
            ex = ex.InnerException;
        }

        return ex.Message;
    }
}
