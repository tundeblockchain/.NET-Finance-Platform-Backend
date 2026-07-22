using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Positions;
using FinancePlatform.Services.Pricing;

namespace FinancePlatform.Services.Portfolio;

public sealed class PortfolioService(
    IPositionService positionService,
    IAssetPriceService assetPriceService,
    ICashService cashService) : IPortfolioService
{
    public PortfolioSnapshot GetPortfolio(Guid tradingAccountId, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        var holdings = positionService.GetByAccount(tradingAccountId);
        var prices = assetPriceService.GetLatestMany(holdings.Select(h => h.AssetSymbol));

        var lines = new List<PortfolioPositionLine>(holdings.Count);
        var positionsValue = 0m;

        foreach (var holding in holdings)
        {
            prices.TryGetValue(holding.AssetSymbol, out var price);
            decimal? marketValue = price is null ? null : holding.Quantity * price.Price;
            if (marketValue is { } value)
            {
                positionsValue += value;
            }

            lines.Add(new PortfolioPositionLine(
                holding.AssetSymbol,
                holding.Quantity,
                price?.Price,
                marketValue,
                price?.ObservedUtc,
                price?.Source.ToString(),
                price?.Provider));
        }

        var cashAvailable = cashService.GetAvailable(tradingAccountId, currency);

        return new PortfolioSnapshot(
            tradingAccountId,
            currency.Trim().ToUpperInvariant(),
            cashAvailable,
            positionsValue,
            cashAvailable + positionsValue,
            lines);
    }
}
