using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Pricing;

public interface IAssetPriceService
{
    AssetPrice Record(
        string assetSymbol,
        decimal price,
        string currency,
        AssetPriceSource source,
        string provider,
        Guid? orderId = null,
        string? externalOrderId = null,
        DateTimeOffset? observedUtc = null);

    AssetPrice? GetLatest(string assetSymbol);

    IReadOnlyDictionary<string, AssetPrice> GetLatestMany(IEnumerable<string> assetSymbols);
}
