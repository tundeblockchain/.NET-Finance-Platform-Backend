namespace FinancePlatform.Services.Portfolio;

public interface IPortfolioService
{
    PortfolioSnapshot GetPortfolio(Guid tradingAccountId, string currency);
}

public sealed record PortfolioPositionLine(
    string AssetSymbol,
    decimal Quantity,
    decimal? LastPrice,
    decimal? MarketValue,
    DateTimeOffset? PriceObservedUtc,
    string? PriceSource,
    string? PriceProvider);

public sealed record PortfolioSnapshot(
    Guid TradingAccountId,
    string Currency,
    decimal CashAvailable,
    decimal PositionsMarketValue,
    decimal TotalEquity,
    IReadOnlyList<PortfolioPositionLine> Positions);
