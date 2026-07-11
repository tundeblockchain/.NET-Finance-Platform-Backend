namespace FinancePlatform.Models.Enums;

/// <summary>
/// Logical platform components that own trigger code ranges.
/// </summary>
public enum ComponentType
{
    Customer = 1,
    Trading = 2,
    Investment = 3,
    AssetTrading = 4,
    Settlement = 5,
    Ledger = 6,
    Pension = 7,
    Insurance = 8
}
