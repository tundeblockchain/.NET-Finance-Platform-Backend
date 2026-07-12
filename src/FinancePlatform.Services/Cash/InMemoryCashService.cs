using FinancePlatform.Models;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Services.Cash;

/// <summary>
/// In-memory cash service with locks, reservations, and idempotent deposits.
/// </summary>
public sealed class InMemoryCashService : ICashService
{
    private readonly object _gate = new();
    private readonly Dictionary<(Guid AccountId, string Currency), CashBalance> _balances = new();
    private readonly Dictionary<string, CashReservation> _reservations = new(StringComparer.Ordinal);
    private readonly HashSet<string> _depositKeys = new(StringComparer.Ordinal);

    public CashBalance GetOrCreateBalance(Guid accountId, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        lock (_gate)
        {
            return Clone(GetOrCreateBalanceUnsafe(accountId, currency.ToUpperInvariant()));
        }
    }

    public decimal GetAvailable(Guid accountId, string currency)
    {
        lock (_gate)
        {
            var key = Key(accountId, currency);
            return _balances.TryGetValue(key, out var balance) ? balance.Available : 0m;
        }
    }

    public decimal GetSettled(Guid accountId, string currency)
    {
        lock (_gate)
        {
            var key = Key(accountId, currency);
            return _balances.TryGetValue(key, out var balance) ? balance.Settled : 0m;
        }
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

        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            var balance = GetOrCreateBalanceUnsafe(accountId, currency.ToUpperInvariant());

            if (balance.IsLocked
                && balance.LockedByTriggerId == triggerId
                && (balance.LockExpiresUtc is null || balance.LockExpiresUtc > now))
            {
                RenewLock(balance, triggerId, allocationRequestId, leaseDuration, now);
                return CashLockResult.AlreadyOwned(Clone(balance));
            }

            var lockFree = !balance.IsLocked
                || balance.LockExpiresUtc is null
                || balance.LockExpiresUtc <= now;

            if (!lockFree)
            {
                return CashLockResult.Contended();
            }

            RenewLock(balance, triggerId, allocationRequestId, leaseDuration, now);
            return CashLockResult.Acquired(Clone(balance));
        }
    }

    public bool TryReleaseLock(Guid accountId, string currency, Guid triggerId)
    {
        lock (_gate)
        {
            var key = Key(accountId, currency);
            if (!_balances.TryGetValue(key, out var balance))
            {
                return false;
            }

            if (!balance.IsLocked || balance.LockedByTriggerId != triggerId)
            {
                return false;
            }

            balance.IsLocked = false;
            balance.LockedByAllocationId = null;
            balance.LockedByTriggerId = null;
            balance.LockAcquiredUtc = null;
            balance.LockExpiresUtc = null;
            Touch(balance);
            return true;
        }
    }

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

        lock (_gate)
        {
            var balance = GetOrCreateBalanceUnsafe(accountId, currency.ToUpperInvariant());

            if (_depositKeys.Contains(idempotencyKey))
            {
                return CashMutationResult.Duplicate(Clone(balance));
            }

            if (!OwnsActiveLock(balance, triggerId))
            {
                return CashMutationResult.Fail("Deposit requires an active cash lock owned by the trigger.");
            }

            balance.Settled += amount;
            _depositKeys.Add(idempotencyKey);
            Touch(balance);
            return CashMutationResult.Success(Clone(balance));
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

        lock (_gate)
        {
            var normalizedCurrency = currency.ToUpperInvariant();
            var balance = GetOrCreateBalanceUnsafe(accountId, normalizedCurrency);

            if (_reservations.TryGetValue(idempotencyKey, out var existing))
            {
                return CashMutationResult.Duplicate(Clone(balance), CloneReservation(existing));
            }

            if (!OwnsActiveLock(balance, triggerId))
            {
                return CashMutationResult.Fail("Reservation requires an active cash lock owned by the trigger.");
            }

            if (balance.Available < amount)
            {
                return CashMutationResult.Fail(
                    $"Insufficient available cash. Available={balance.Available}, requested={amount}.");
            }

            var now = DateTimeOffset.UtcNow;
            var reservation = new CashReservation
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                AllocationRequestId = allocationRequestId,
                TriggerId = triggerId,
                Currency = normalizedCurrency,
                Amount = amount,
                IdempotencyKey = idempotencyKey,
                IsReleased = false,
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.Broker
            };

            balance.Reserved += amount;
            _reservations[idempotencyKey] = reservation;
            Touch(balance);
            return CashMutationResult.Success(Clone(balance), CloneReservation(reservation));
        }
    }

    public CashMutationResult TryReleaseReservation(string idempotencyKey, Guid triggerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        lock (_gate)
        {
            if (!_reservations.TryGetValue(idempotencyKey, out var reservation))
            {
                return CashMutationResult.Fail("Reservation was not found.");
            }

            var balance = GetOrCreateBalanceUnsafe(reservation.AccountId, reservation.Currency);
            if (!OwnsActiveLock(balance, triggerId))
            {
                return CashMutationResult.Fail("Release reservation requires an active cash lock owned by the trigger.");
            }

            if (reservation.IsReleased)
            {
                return CashMutationResult.Duplicate(Clone(balance), CloneReservation(reservation));
            }

            balance.Reserved -= reservation.Amount;
            if (balance.Reserved < 0)
            {
                balance.Reserved = 0;
            }

            reservation.IsReleased = true;
            reservation.ReleasedUtc = DateTimeOffset.UtcNow;
            reservation.DateModified = reservation.ReleasedUtc.Value;
            reservation.ChangedBy = ChangeActors.Broker;
            Touch(balance);
            return CashMutationResult.Success(Clone(balance), CloneReservation(reservation));
        }
    }

    public CashMutationResult TryConsumeReservation(string idempotencyKey, Guid triggerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        lock (_gate)
        {
            if (!_reservations.TryGetValue(idempotencyKey, out var reservation))
            {
                return CashMutationResult.Fail("Reservation was not found.");
            }

            var balance = GetOrCreateBalanceUnsafe(reservation.AccountId, reservation.Currency);
            if (!OwnsActiveLock(balance, triggerId))
            {
                return CashMutationResult.Fail("Consume reservation requires an active cash lock owned by the trigger.");
            }

            if (reservation.IsReleased)
            {
                return CashMutationResult.Duplicate(Clone(balance), CloneReservation(reservation));
            }

            balance.Settled -= reservation.Amount;
            balance.Reserved -= reservation.Amount;
            if (balance.Settled < 0 || balance.Reserved < 0)
            {
                return CashMutationResult.Fail("Cash balance would become inconsistent.");
            }

            reservation.IsReleased = true;
            reservation.ReleasedUtc = DateTimeOffset.UtcNow;
            reservation.DateModified = reservation.ReleasedUtc.Value;
            reservation.ChangedBy = ChangeActors.Broker;
            Touch(balance);
            return CashMutationResult.Success(Clone(balance), CloneReservation(reservation));
        }
    }

    private CashBalance GetOrCreateBalanceUnsafe(Guid accountId, string currency)
    {
        var key = (accountId, currency);
        if (_balances.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var created = new CashBalance
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Currency = currency,
            Settled = 0m,
            Reserved = 0m,
            DateModified = now,
            ChangedBy = ChangeActors.Broker
        };
        _balances[key] = created;
        return created;
    }

    private static void RenewLock(
        CashBalance balance,
        Guid triggerId,
        Guid? allocationRequestId,
        TimeSpan leaseDuration,
        DateTimeOffset now)
    {
        balance.IsLocked = true;
        balance.LockedByTriggerId = triggerId;
        balance.LockedByAllocationId = allocationRequestId;
        balance.LockAcquiredUtc ??= now;
        balance.LockExpiresUtc = now.Add(leaseDuration);
        Touch(balance);
    }

    private static bool OwnsActiveLock(CashBalance balance, Guid triggerId)
    {
        var now = DateTimeOffset.UtcNow;
        return balance.IsLocked
            && balance.LockedByTriggerId == triggerId
            && (balance.LockExpiresUtc is null || balance.LockExpiresUtc > now);
    }

    private static void Touch(CashBalance balance)
    {
        balance.DateModified = DateTimeOffset.UtcNow;
        balance.ChangedBy = ChangeActors.Broker;
    }

    private static (Guid AccountId, string Currency) Key(Guid accountId, string currency) =>
        (accountId, currency.ToUpperInvariant());

    private static CashBalance Clone(CashBalance source) => new()
    {
        Id = source.Id,
        AccountId = source.AccountId,
        Currency = source.Currency,
        Settled = source.Settled,
        Reserved = source.Reserved,
        IsLocked = source.IsLocked,
        LockedByAllocationId = source.LockedByAllocationId,
        LockedByTriggerId = source.LockedByTriggerId,
        LockAcquiredUtc = source.LockAcquiredUtc,
        LockExpiresUtc = source.LockExpiresUtc,
        DateModified = source.DateModified,
        ChangedBy = source.ChangedBy
    };

    private static CashReservation CloneReservation(CashReservation source) => new()
    {
        Id = source.Id,
        AccountId = source.AccountId,
        AllocationRequestId = source.AllocationRequestId,
        TriggerId = source.TriggerId,
        Currency = source.Currency,
        Amount = source.Amount,
        IdempotencyKey = source.IdempotencyKey,
        IsReleased = source.IsReleased,
        CreatedUtc = source.CreatedUtc,
        ReleasedUtc = source.ReleasedUtc,
        DateModified = source.DateModified,
        ChangedBy = source.ChangedBy
    };
}
