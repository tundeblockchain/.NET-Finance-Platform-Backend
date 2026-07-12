namespace FinancePlatform.Models.Triggers;

/// <summary>
/// Queue names used by the service broker.
/// </summary>
public static class QueueNames
{
    public const string Cash = "Cash";
    public const string Trading = "Trading";
    public const string Customer = "Customer";
    public const string Investment = "Investment";
    public const string AssetTrading = "AssetTrading";
    public const string Settlement = "Settlement";
    public const string Reversal = "Reversal";
}
