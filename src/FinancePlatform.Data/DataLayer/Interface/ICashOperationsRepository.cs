using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface ICashOperationsRepository
{
    Task<CashBalance?> GetBalanceByAccountCurrencyAsync(
        Guid accountId,
        string currency,
        CancellationToken cancellationToken = default);

    Task<CashBalance> EnsureBalanceAsync(
        Guid accountId,
        string currency,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<CashBalance?> AcquireLockAsync(
        Guid accountId,
        string currency,
        Guid triggerId,
        Guid? allocationRequestId,
        int leaseSeconds,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<bool> ReleaseLockAsync(
        Guid accountId,
        string currency,
        Guid triggerId,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<(CashBalance Balance, bool AlreadyApplied)> DepositAsync(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        Guid triggerId,
        Guid? allocationRequestId,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<(CashBalance Balance, CashReservation Reservation, bool AlreadyApplied)> ReserveAsync(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        Guid triggerId,
        Guid allocationRequestId,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<(CashBalance Balance, CashReservation Reservation, bool AlreadyApplied)> ReleaseReservationAsync(
        string idempotencyKey,
        Guid triggerId,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<(CashBalance Balance, CashReservation Reservation, bool AlreadyApplied)> ConsumeReservationAsync(
        string idempotencyKey,
        Guid triggerId,
        string changedBy,
        CancellationToken cancellationToken = default);
}
