namespace FinancePlatform.Models.Enums;

/// <summary>
/// Discriminator for the polymorphic external reference on a trigger context.
/// </summary>
public enum ExternalEntityType
{
    Customer = 1,
    Account = 2,
    Transfer = 3,
    CustomerAccount = 4,
    TradingAccount = 5,
    InvestmentAccount = 6
}
