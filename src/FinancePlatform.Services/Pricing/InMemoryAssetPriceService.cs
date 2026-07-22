using System.Collections.Concurrent;
using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Pricing;

public sealed class InMemoryAssetPriceService : IAssetPriceService
{
    private readonly ConcurrentDictionary<Guid, AssetPrice> _byId = new();
    private readonly ConcurrentDictionary<string, AssetPrice> _latestBySymbol = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public AssetPrice Record(
        string assetSymbol,
        decimal price,
        string currency,
        AssetPriceSource source,
        string provider,
        Guid? orderId = null,
        string? externalOrderId = null,
        DateTimeOffset? observedUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive.");
        }

        var now = observedUtc ?? DateTimeOffset.UtcNow;
        var entity = new AssetPrice
        {
            Id = Guid.NewGuid(),
            AssetSymbol = assetSymbol.Trim().ToUpperInvariant(),
            Price = price,
            Currency = currency.Trim().ToUpperInvariant(),
            Source = source,
            Provider = provider,
            OrderId = orderId,
            ExternalOrderId = externalOrderId,
            ObservedUtc = now,
            DateModified = now,
            ChangedBy = ChangeActors.Broker
        };

        lock (_gate)
        {
            _byId[entity.Id] = entity;
            if (!_latestBySymbol.TryGetValue(entity.AssetSymbol, out var current)
                || entity.ObservedUtc >= current.ObservedUtc)
            {
                _latestBySymbol[entity.AssetSymbol] = entity;
            }
        }

        return Clone(entity);
    }

    public AssetPrice? GetLatest(string assetSymbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        return _latestBySymbol.TryGetValue(assetSymbol.Trim(), out var price) ? Clone(price) : null;
    }

    public IReadOnlyDictionary<string, AssetPrice> GetLatestMany(IEnumerable<string> assetSymbols)
    {
        var result = new Dictionary<string, AssetPrice>(StringComparer.OrdinalIgnoreCase);
        foreach (var symbol in assetSymbols.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (_latestBySymbol.TryGetValue(symbol.Trim(), out var price))
            {
                result[price.AssetSymbol] = Clone(price);
            }
        }

        return result;
    }

    private static AssetPrice Clone(AssetPrice p) => new()
    {
        Id = p.Id,
        AssetSymbol = p.AssetSymbol,
        Price = p.Price,
        Currency = p.Currency,
        Source = p.Source,
        Provider = p.Provider,
        OrderId = p.OrderId,
        ExternalOrderId = p.ExternalOrderId,
        ObservedUtc = p.ObservedUtc,
        DateModified = p.DateModified,
        ChangedBy = p.ChangedBy
    };
}
