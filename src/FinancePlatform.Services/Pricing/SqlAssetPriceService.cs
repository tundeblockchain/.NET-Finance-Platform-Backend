using FinancePlatform.Data.DataLayer;
using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Pricing;

public sealed class SqlAssetPriceService(IAssetPriceRepository repository) : IAssetPriceService
{
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

        return repository.UpsertAsync(entity).GetAwaiter().GetResult();
    }

    public AssetPrice? GetLatest(string assetSymbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        return repository.GetLatestAsync(assetSymbol).GetAwaiter().GetResult();
    }

    public IReadOnlyDictionary<string, AssetPrice> GetLatestMany(IEnumerable<string> assetSymbols)
    {
        var result = new Dictionary<string, AssetPrice>(StringComparer.OrdinalIgnoreCase);
        foreach (var symbol in assetSymbols.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var price = GetLatest(symbol);
            if (price is not null)
            {
                result[price.AssetSymbol] = price;
            }
        }

        return result;
    }
}
