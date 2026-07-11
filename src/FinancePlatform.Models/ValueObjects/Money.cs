namespace FinancePlatform.Models.ValueObjects;

/// <summary>
/// Monetary amount with an ISO currency code.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);

    public Money EnsureNonNegative()
    {
        if (Amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(Amount), Amount, "Money amount cannot be negative.");
        }

        return this;
    }
}
