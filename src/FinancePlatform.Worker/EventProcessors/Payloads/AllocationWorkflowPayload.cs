namespace FinancePlatform.Worker.EventProcessors.Payloads;

public sealed class AllocationWorkflowPayload
{
    public decimal Amount { get; set; }

    public decimal CashAmount { get; set; }

    public string Currency { get; set; } = "GBP";

    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;

    public decimal EffectiveCashAmount => CashAmount > 0 ? CashAmount : Amount;
}
