using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinancePlatform.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancePlatform.Services.Brokers.Alpaca;

public sealed class AlpacaBrokerTradingProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<AlpacaBrokerOptions> options,
    ILogger<AlpacaBrokerTradingProvider> logger) : IBrokerTradingProvider
{
    public const string Name = "Alpaca";
    public const string TradingClientName = "AlpacaTrading";
    public const string DataClientName = "AlpacaData";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ProviderName => Name;

    public async Task<BrokerQuote> GetQuoteAsync(
        string assetSymbol,
        decimal? referencePrice,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        var symbol = assetSymbol.Trim().ToUpperInvariant();
        var client = httpClientFactory.CreateClient(DataClientName);

        using var response = await client.GetAsync(
            $"v2/stocks/{Uri.EscapeDataString(symbol)}/quotes/latest",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Alpaca quote failed for {symbol}: {(int)response.StatusCode} {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<AlpacaLatestQuoteResponse>(
            JsonOptions,
            cancellationToken);

        var quote = payload?.Quote
            ?? throw new InvalidOperationException($"Alpaca returned no quote for {symbol}.");

        var bid = quote.Bp;
        var ask = quote.Ap;
        if (bid <= 0 && ask <= 0)
        {
            if (referencePrice is > 0)
            {
                logger.LogWarning(
                    "Alpaca quote for {Symbol} had no bid/ask; falling back to reference price {Price}.",
                    symbol,
                    referencePrice);
                bid = ask = referencePrice.Value;
            }
            else
            {
                throw new InvalidOperationException($"Alpaca quote for {symbol} had no usable bid/ask.");
            }
        }
        else if (bid <= 0)
        {
            bid = ask;
        }
        else if (ask <= 0)
        {
            ask = bid;
        }

        var mid = (bid + ask) / 2m;
        var observed = quote.T == default ? DateTimeOffset.UtcNow : quote.T;

        return new BrokerQuote(symbol, bid, ask, mid, observed, Name);
    }

    public async Task<BrokerOrderExecution> PlaceMarketOrderAsync(
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

        var opts = options.Value;
        var symbol = request.AssetSymbol.Trim().ToUpperInvariant();
        var client = httpClientFactory.CreateClient(TradingClientName);

        var side = request.Side == OrderSide.Buy ? "buy" : "sell";
        var createBody = new
        {
            symbol,
            qty = request.Quantity.ToString(CultureInfo.InvariantCulture),
            side,
            type = "market",
            time_in_force = "day",
            client_order_id = request.ClientOrderId
        };

        using var createResponse = await client.PostAsJsonAsync("v2/orders", createBody, cancellationToken);
        var createJson = await createResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!createResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Alpaca order submit failed for {symbol}: {(int)createResponse.StatusCode} {createJson}");
        }

        var order = JsonSerializer.Deserialize<AlpacaOrderResponse>(createJson, JsonOptions)
            ?? throw new InvalidOperationException("Alpaca order response was empty.");

        order = await WaitForFillAsync(client, order, opts, cancellationToken);

        if (!string.Equals(order.Status, "filled", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(order.Status, "partially_filled", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Alpaca order {order.Id} ended in status '{order.Status}' without a fill.");
        }

        var fillPrice = ParseDecimal(order.FilledAvgPrice)
            ?? request.ReferencePrice
            ?? throw new InvalidOperationException($"Alpaca order {order.Id} has no fill price.");

        var filledUtc = order.FilledAt ?? order.UpdatedAt ?? DateTimeOffset.UtcNow;
        var filledQty = ParseDecimal(order.FilledQty) ?? request.Quantity;

        return new BrokerOrderExecution(
            ExternalOrderId: order.Id ?? request.ClientOrderId,
            AssetSymbol: symbol,
            Side: request.Side,
            Quantity: filledQty,
            AverageFillPrice: fillPrice,
            FilledUtc: filledUtc,
            Provider: Name,
            Status: order.Status ?? "filled");
    }

    private async Task<AlpacaOrderResponse> WaitForFillAsync(
        HttpClient client,
        AlpacaOrderResponse order,
        AlpacaBrokerOptions opts,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < opts.FillPollAttempts; attempt++)
        {
            if (string.Equals(order.Status, "filled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(order.Status, "partially_filled", StringComparison.OrdinalIgnoreCase)
                    && ParseDecimal(order.FilledAvgPrice) is > 0)
            {
                if (string.Equals(order.Status, "filled", StringComparison.OrdinalIgnoreCase))
                {
                    return order;
                }
            }

            if (string.Equals(order.Status, "canceled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(order.Status, "expired", StringComparison.OrdinalIgnoreCase)
                || string.Equals(order.Status, "rejected", StringComparison.OrdinalIgnoreCase))
            {
                return order;
            }

            await Task.Delay(opts.FillPollDelayMilliseconds, cancellationToken);

            var id = order.Id
                ?? throw new InvalidOperationException("Alpaca order id was missing while polling fill status.");

            using var response = await client.GetAsync($"v2/orders/{Uri.EscapeDataString(id)}", cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Alpaca order poll failed for {OrderId}: {Status} {Body}",
                    id,
                    (int)response.StatusCode,
                    json);
                continue;
            }

            order = JsonSerializer.Deserialize<AlpacaOrderResponse>(json, JsonOptions) ?? order;
        }

        return order;
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;

    private sealed class AlpacaLatestQuoteResponse
    {
        public AlpacaQuote? Quote { get; set; }
    }

    private sealed class AlpacaQuote
    {
        public decimal Ap { get; set; }

        public decimal Bp { get; set; }

        public DateTimeOffset T { get; set; }
    }

    private sealed class AlpacaOrderResponse
    {
        public string? Id { get; set; }

        public string? Status { get; set; }

        public string? FilledAvgPrice { get; set; }

        public string? FilledQty { get; set; }

        public DateTimeOffset? FilledAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
