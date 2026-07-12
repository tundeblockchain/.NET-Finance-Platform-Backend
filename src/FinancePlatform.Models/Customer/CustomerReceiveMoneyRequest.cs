namespace FinancePlatform.Models.Customer;

/// <summary>
/// Payload for Customer.ReceiveMoney (6003) when funds return from the trading account.
/// </summary>
public sealed class CustomerReceiveMoneyRequest
{
    public int CustomerId { get; set; }

    public Guid CustomerAccountId { get; set; }

    public Guid? SourceTradingAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "GBP";
}
