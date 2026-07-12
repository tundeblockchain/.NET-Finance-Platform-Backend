using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Ledger;

public sealed class InMemoryLedgerService : ILedgerService
{
    private readonly object _gate = new();
    private readonly Dictionary<string, LedgerEntry> _entries = new(StringComparer.Ordinal);

    public int EntryCount
    {
        get
        {
            lock (_gate)
            {
                return _entries.Count;
            }
        }
    }

    public LedgerEntry? FindByIdempotencyKey(string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        lock (_gate)
        {
            return _entries.TryGetValue(idempotencyKey, out var entry) ? Clone(entry) : null;
        }
    }

    public LedgerPostResult TryPost(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        LedgerEntryType entryType,
        string description,
        Guid? triggerId,
        Guid? allocationRequestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (amount <= 0)
        {
            return LedgerPostResult.Fail("Ledger amount must be positive.");
        }

        lock (_gate)
        {
            if (_entries.TryGetValue(idempotencyKey, out var existing))
            {
                return LedgerPostResult.Duplicate(Clone(existing));
            }

            var now = DateTimeOffset.UtcNow;
            var entry = new LedgerEntry
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                TriggerId = triggerId,
                AllocationRequestId = allocationRequestId,
                EntryType = entryType,
                Amount = amount,
                Currency = currency.ToUpperInvariant(),
                IdempotencyKey = idempotencyKey,
                Description = description,
                PostedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.Broker
            };

            _entries[idempotencyKey] = entry;
            return LedgerPostResult.Success(Clone(entry));
        }
    }

    private static LedgerEntry Clone(LedgerEntry source) => new()
    {
        Id = source.Id,
        AccountId = source.AccountId,
        TriggerId = source.TriggerId,
        AllocationRequestId = source.AllocationRequestId,
        EntryType = source.EntryType,
        Amount = source.Amount,
        Currency = source.Currency,
        IdempotencyKey = source.IdempotencyKey,
        Description = source.Description,
        PostedUtc = source.PostedUtc,
        DateModified = source.DateModified,
        ChangedBy = source.ChangedBy
    };
}
