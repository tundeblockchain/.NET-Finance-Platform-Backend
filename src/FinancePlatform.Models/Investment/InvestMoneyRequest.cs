namespace FinancePlatform.Models.Investment;

/// <summary>
/// Investment component money request (triggers 8001 / 8002).
/// </summary>
public sealed class InvestMoneyRequest
{
    public Guid InstructionId { get; set; }

    public int CustomerId { get; set; }

    public Guid TradingAccountId { get; set; }

    public Guid InvestmentAccountId { get; set; }

    public decimal Amount { get; set; }

    public decimal CashAmount { get; set; }

    public string Currency { get; set; } = "GBP";

    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;

    public decimal EffectiveCashAmount => CashAmount > 0 ? CashAmount : Amount;
}
