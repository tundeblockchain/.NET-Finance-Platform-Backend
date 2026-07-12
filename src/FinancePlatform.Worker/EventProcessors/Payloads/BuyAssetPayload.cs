namespace FinancePlatform.Worker.EventProcessors.Payloads;

public sealed class BuyAssetPayload
{
    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;

    public string Currency { get; set; } = "GBP";

    public decimal CashAmount { get; set; }
}
