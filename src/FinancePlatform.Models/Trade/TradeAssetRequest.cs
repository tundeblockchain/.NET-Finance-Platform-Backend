namespace FinancePlatform.Models.Trade;

/// <summary>
/// Classic trade buy/sell request (triggers 2002 / 2003).
/// Cash is derived from broker quote/fill when <see cref="CashAmount"/> is omitted.
/// </summary>
public sealed class TradeAssetRequest
{
    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;

    public string Currency { get; set; } = "GBP";

    /// <summary>
    /// Optional. When set, used only as a price hint for simulated quotes / reversals.
    /// Live cash movement uses broker fill notional.
    /// </summary>
    public decimal? CashAmount { get; set; }
}
