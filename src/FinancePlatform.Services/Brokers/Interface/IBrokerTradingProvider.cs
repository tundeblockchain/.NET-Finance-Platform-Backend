using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Brokers;

/// <summary>
/// External broker port for quotes and order execution.
/// Implementations must never be called while holding platform cash locks.
/// </summary>
public interface IBrokerTradingProvider
{
    string ProviderName { get; }

    Task<BrokerQuote> GetQuoteAsync(
        string assetSymbol,
        decimal? referencePrice,
        CancellationToken cancellationToken = default);

    Task<BrokerOrderExecution> PlaceMarketOrderAsync(
        BrokerMarketOrderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record BrokerQuote(
    string AssetSymbol,
    decimal Bid,
    decimal Ask,
    decimal Mid,
    DateTimeOffset ObservedUtc,
    string Provider);

public sealed record BrokerMarketOrderRequest(
    string AssetSymbol,
    OrderSide Side,
    decimal Quantity,
    string ClientOrderId,
    decimal? ReferencePrice);

public sealed record BrokerOrderExecution(
    string ExternalOrderId,
    string AssetSymbol,
    OrderSide Side,
    decimal Quantity,
    decimal AverageFillPrice,
    DateTimeOffset FilledUtc,
    string Provider,
    string Status);
