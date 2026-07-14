namespace FinancePlatform.Models.Asset;

/// <summary>
/// Asset trading buy/sell request (triggers 9001 / 9002).
/// </summary>
public sealed class AssetOrderRequest
{
    public Guid InstructionId { get; set; }

    public Guid OrderId { get; set; }

    public Guid InvestmentAccountId { get; set; }

    public decimal Amount { get; set; }

    public decimal CashAmount { get; set; }

    public string Currency { get; set; } = "GBP";

    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;

    public decimal EffectiveCashAmount => CashAmount > 0 ? CashAmount : Amount;
}
