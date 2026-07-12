namespace FinancePlatform.Worker.Handlers;

public sealed class BuyAssetPayload
{
    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;
}
