namespace FinancePlatform.Models.ValueObjects;

/// <summary>
/// Stable key used to make financial operations idempotent across retries.
/// </summary>
public readonly record struct IdempotencyKey
{
    public string Value { get; }

    public IdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public override string ToString() => Value;

    public static implicit operator string(IdempotencyKey key) => key.Value;
}
