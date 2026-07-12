namespace FinancePlatform.Models.Customer;

/// <summary>
/// Distribute from customer account to trading account (park-only path).
/// </summary>
public sealed class DistributeMoneyRequest
{
    public int CustomerId { get; set; }

    public Guid CustomerAccountId { get; set; }

    public Guid TradingAccountId { get; set; }

    public decimal Amount { get; set; }

    public decimal CashAmount { get; set; }

    public string Currency { get; set; } = "GBP";

    public decimal EffectiveAmount => CashAmount > 0 ? CashAmount : Amount;
}
