using System.Collections.Concurrent;

namespace FinancePlatform.Services.Cash;

/// <summary>
/// Phase 2 in-memory cash service used by sample handlers and idempotency tests.
/// </summary>
public sealed class InMemoryCashService : ICashService
{
    private readonly ConcurrentDictionary<string, DepositRecord> _deposits = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, decimal> _balances = new();

    public bool TryDeposit(string idempotencyKey, Guid accountId, decimal amount, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive.");
        }

        var added = _deposits.TryAdd(idempotencyKey, new DepositRecord(accountId, amount, currency));
        if (!added)
        {
            return false;
        }

        _balances.AddOrUpdate(accountId, amount, (_, current) => current + amount);
        return true;
    }

    public decimal GetBalance(Guid accountId) =>
        _balances.TryGetValue(accountId, out var balance) ? balance : 0m;

    public int DepositCount => _deposits.Count;

    private sealed record DepositRecord(Guid AccountId, decimal Amount, string Currency);
}
