namespace FinancePlatform.Models.Customer;

/// <summary>
/// Customer distribute-money request (trigger 6002).
/// </summary>
public sealed class DistributeMoneyRequest
{
    public decimal Amount { get; set; }

    public decimal CashAmount { get; set; }

    public string Currency { get; set; } = "GBP";

    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;

    public decimal EffectiveCashAmount => CashAmount > 0 ? CashAmount : Amount;
}
