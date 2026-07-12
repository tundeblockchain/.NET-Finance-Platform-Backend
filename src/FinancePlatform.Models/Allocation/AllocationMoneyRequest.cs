namespace FinancePlatform.Models.Allocation;

/// <summary>
/// Money movement payload carried through the allocation trigger chain.
/// </summary>
public sealed class AllocationMoneyRequest
{
    public decimal Amount { get; set; }

    public decimal CashAmount { get; set; }

    public string Currency { get; set; } = "GBP";

    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;

    public decimal EffectiveCashAmount => CashAmount > 0 ? CashAmount : Amount;
}
