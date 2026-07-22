using System.Collections.Concurrent;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Brokers;

/// <summary>
/// Local broker used by tests and when no external provider is configured.
/// Uses a fixed default unit price when no reference price is supplied.
/// </summary>
public sealed class SimulatedBrokerTradingProvider : IBrokerTradingProvider
{
    public const string Name = "Simulated";

    /// <summary>
    /// Default mark used when callers do not supply a reference price.
    /// </summary>
    public const decimal DefaultUnitPrice = 100m;

    private readonly ConcurrentDictionary<string, BrokerOrderExecution> _executionsByClientOrderId =
        new(StringComparer.Ordinal);

    public string ProviderName => Name;

    public Task<BrokerQuote> GetQuoteAsync(
        string assetSymbol,
        decimal? referencePrice,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        var price = referencePrice is > 0 ? referencePrice.Value : DefaultUnitPrice;

        var now = DateTimeOffset.UtcNow;
        return Task.FromResult(new BrokerQuote(
            assetSymbol.Trim().ToUpperInvariant(),
            Bid: price,
            Ask: price,
            Mid: price,
            ObservedUtc: now,
            Provider: Name));
    }

    public Task<BrokerOrderExecution> PlaceMarketOrderAsync(
        BrokerMarketOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AssetSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ClientOrderId);
        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Quantity must be positive.");
        }

        if (_executionsByClientOrderId.TryGetValue(request.ClientOrderId, out var existing))
        {
            return Task.FromResult(existing);
        }

        var fillPrice = request.ReferencePrice is > 0
            ? request.ReferencePrice.Value
            : DefaultUnitPrice;

        var now = DateTimeOffset.UtcNow;
        var execution = new BrokerOrderExecution(
            ExternalOrderId: $"sim-{request.ClientOrderId}",
            AssetSymbol: request.AssetSymbol.Trim().ToUpperInvariant(),
            Side: request.Side,
            Quantity: request.Quantity,
            AverageFillPrice: fillPrice,
            FilledUtc: now,
            Provider: Name,
            Status: "filled");

        return Task.FromResult(_executionsByClientOrderId.GetOrAdd(request.ClientOrderId, execution));
    }
}
