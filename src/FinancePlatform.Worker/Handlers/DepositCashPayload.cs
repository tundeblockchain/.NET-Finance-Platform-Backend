namespace FinancePlatform.Worker.Handlers;

public sealed class DepositCashPayload
{
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "GBP";

    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;
}
