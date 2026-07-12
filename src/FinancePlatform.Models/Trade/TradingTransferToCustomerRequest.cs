namespace FinancePlatform.Models.Trade;

/// <summary>
/// Payload for Trading.TransferToCustomer (7003) — moves parked funds back to the customer account.
/// </summary>
public sealed class TradingTransferToCustomerRequest
{
    public int CustomerId { get; set; }

    public Guid TradingAccountId { get; set; }

    public Guid CustomerAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "GBP";
}
