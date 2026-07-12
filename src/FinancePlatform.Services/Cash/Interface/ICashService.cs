using FinancePlatform.Models.Entities;

namespace FinancePlatform.Services.Cash;

public interface ICashService
{
    CashBalance GetOrCreateBalance(Guid accountId, string currency);

    decimal GetAvailable(Guid accountId, string currency);

    decimal GetSettled(Guid accountId, string currency);

    /// <summary>
    /// Atomically acquires the cash lock. Does not wait when contended.
    /// </summary>
    CashLockResult TryAcquireLock(
        Guid accountId,
        string currency,
        Guid triggerId,
        Guid? allocationRequestId,
        TimeSpan leaseDuration);

    /// <summary>
    /// Releases the lock only when owned by <paramref name="triggerId"/>.
    /// </summary>
    bool TryReleaseLock(Guid accountId, string currency, Guid triggerId);

    /// <summary>
    /// Credits settled cash while holding the lock. Idempotent by key.
    /// </summary>
    CashMutationResult TryDeposit(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        Guid triggerId);

    /// <summary>
    /// Reserves available cash while holding the lock. Idempotent by key.
    /// </summary>
    CashMutationResult TryReserve(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        Guid triggerId,
        Guid allocationRequestId);

    /// <summary>
    /// Releases a reservation (returns reserved to available) while holding the lock.
    /// </summary>
    CashMutationResult TryReleaseReservation(string idempotencyKey, Guid triggerId);

    /// <summary>
    /// Consumes a reservation (reduces Settled and Reserved) while holding the lock.
    /// </summary>
    CashMutationResult TryConsumeReservation(string idempotencyKey, Guid triggerId);
}
