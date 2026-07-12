using System.Collections.Concurrent;

namespace FinancePlatform.Services.Positions;

public sealed class InMemoryPositionService : IPositionService
{
    private readonly ConcurrentDictionary<(Guid AccountId, string Symbol), decimal> _positions = new();
    private readonly ConcurrentDictionary<string, byte> _appliedKeys = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public decimal GetQuantity(Guid accountId, string assetSymbol) =>
        _positions.TryGetValue((accountId, assetSymbol), out var qty) ? qty : 0m;

    public bool TryApplyBuy(string idempotencyKey, Guid accountId, string assetSymbol, decimal quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        lock (_gate)
        {
            if (!_appliedKeys.TryAdd(idempotencyKey, 0))
            {
                return false;
            }

            _positions.AddOrUpdate((accountId, assetSymbol), quantity, (_, current) => current + quantity);
            return true;
        }
    }

    public PositionMutationResult TryApplySell(
        string idempotencyKey,
        Guid accountId,
        string assetSymbol,
        decimal quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        lock (_gate)
        {
            if (_appliedKeys.ContainsKey(idempotencyKey))
            {
                return PositionMutationResult.Success(GetQuantity(accountId, assetSymbol), alreadyApplied: true);
            }

            var current = GetQuantity(accountId, assetSymbol);
            if (current < quantity)
            {
                return PositionMutationResult.Failure(
                    $"Insufficient position for {assetSymbol}. Available={current}, requested={quantity}.");
            }

            _appliedKeys.TryAdd(idempotencyKey, 0);
            var remaining = current - quantity;
            if (remaining == 0)
            {
                _positions.TryRemove((accountId, assetSymbol), out _);
            }
            else
            {
                _positions[(accountId, assetSymbol)] = remaining;
            }

            return PositionMutationResult.Success(remaining);
        }
    }
}
