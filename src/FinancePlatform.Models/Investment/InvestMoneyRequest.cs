namespace FinancePlatform.Models.Investment;

/// <summary>
/// Investment component money request (triggers 8001 / 8002).
/// </summary>
public sealed class InvestMoneyRequest
{
    public decimal Amount { get; set; }

    public decimal CashAmount { get; set; }

    public string Currency { get; set; } = "GBP";

    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;

    public decimal EffectiveCashAmount => CashAmount > 0 ? CashAmount : Amount;
}
