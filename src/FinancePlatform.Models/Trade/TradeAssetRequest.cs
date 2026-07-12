namespace FinancePlatform.Models.Trade;

/// <summary>
/// Classic trade buy/sell request (triggers 2002 / 2003).
/// </summary>
public sealed class TradeAssetRequest
{
    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;

    public string Currency { get; set; } = "GBP";

    public decimal CashAmount { get; set; }
}
