using System.Collections.Concurrent;

namespace FinancePlatform.Services.Trading;

public sealed class InMemoryTradingService : ITradingService
{
    private readonly ConcurrentDictionary<string, BuyRecord> _buys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<(Guid AccountId, string Symbol), decimal> _positions = new();

    public bool TryBuy(string idempotencyKey, Guid accountId, string assetSymbol, decimal quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Buy quantity must be positive.");
        }

        var added = _buys.TryAdd(idempotencyKey, new BuyRecord(accountId, assetSymbol, quantity));
        if (!added)
        {
            return false;
        }

        _positions.AddOrUpdate((accountId, assetSymbol), quantity, (_, current) => current + quantity);
        return true;
    }

    public decimal GetPosition(Guid accountId, string assetSymbol) =>
        _positions.TryGetValue((accountId, assetSymbol), out var qty) ? qty : 0m;

    public int BuyCount => _buys.Count;

    private sealed record BuyRecord(Guid AccountId, string AssetSymbol, decimal Quantity);
}
