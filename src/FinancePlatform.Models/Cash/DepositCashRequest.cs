namespace FinancePlatform.Models.Cash;

/// <summary>
/// Deposit cash request (trigger 1001).
/// </summary>
public sealed class DepositCashRequest
{
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "GBP";

    public string AssetSymbol { get; set; } = "VWRL";

    public decimal Quantity { get; set; } = 1m;
}
