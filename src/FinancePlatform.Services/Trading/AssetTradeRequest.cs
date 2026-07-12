namespace FinancePlatform.Services.Trading;

public sealed class AssetTradeRequest
{
    public required string AssetSymbol { get; init; }

    public required decimal Quantity { get; init; }

    public required string Currency { get; init; }

    public required decimal CashAmount { get; init; }
}
