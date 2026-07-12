namespace FinancePlatform.Models.Trade;

/// <summary>
/// Payload for Trading.ReceiveMoney when funds are parked from the customer account.
/// </summary>
public sealed class TradingReceiveMoneyRequest
{
    public int CustomerId { get; set; }

    public Guid TradingAccountId { get; set; }

    public Guid? SourceCustomerAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "GBP";

    /// <summary>
    /// When true, Trading.Receive only parks funds and does not raise distribute.
    /// </summary>
    public bool ParkOnly { get; set; } = true;
}
